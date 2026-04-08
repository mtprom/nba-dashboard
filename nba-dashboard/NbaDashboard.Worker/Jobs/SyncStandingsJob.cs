using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NbaDashboard.Core.Entities;
using NbaDashboard.Infrastructure.Data;
using NbaDashboard.Infrastructure.NbaStats;
using NbaDashboard.Infrastructure.NbaStats.Models;

namespace NbaDashboard.Worker.Jobs;

public class SyncStandingsJob
{
    private readonly NbaStatsClient _nba;
    private readonly AppDbContext _db;
    private readonly ILogger<SyncStandingsJob> _logger;

    public SyncStandingsJob(NbaStatsClient nba, AppDbContext db, ILogger<SyncStandingsJob> logger)
    {
        _nba = nba;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Sync current season standings. Used by nightly cron.
    /// </summary>
    public async Task RunAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        int seasonYear = now.Month >= 10 ? now.Year : now.Year - 1;
        await SyncSeasonAsync(seasonYear, ct);
    }

    /// <summary>
    /// Sync current + previous season standings. Used on startup to ensure
    /// season-vs-season comparisons have real NBA data for both seasons.
    /// </summary>
    public async Task RunWithPreviousAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        int seasonYear = now.Month >= 10 ? now.Year : now.Year - 1;

        // Sync previous season first (only needs one snapshot — final standings)
        await SyncSeasonAsync(seasonYear - 1, ct);
        // Then current season
        await SyncSeasonAsync(seasonYear, ct);
    }

    private async Task SyncSeasonAsync(int seasonYear, CancellationToken ct)
    {
        var seasonStr = $"{seasonYear}-{(seasonYear + 1) % 100:D2}";
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Idempotency: skip if already synced today
        var cursorKey = $"standings_{seasonStr}_{today:yyyy-MM-dd}";
        if (await _db.SyncStates.AnyAsync(s => s.Key == cursorKey, ct))
        {
            _logger.LogInformation("Standings for {Season} on {Date} already synced, skipping",
                seasonStr, today);
            return;
        }

        _logger.LogInformation("Fetching standings for {Season}", seasonStr);

        var resp = await _nba.GetAsync<LeagueStandingsV3Response>("leaguestandingsv3",
            new Dictionary<string, string>
            {
                ["LeagueID"] = "00",
                ["Season"] = seasonStr,
                ["SeasonType"] = "Regular Season",
            }, ct);

        var resultSet = resp?.ResultSets?.FirstOrDefault();
        if (resultSet == null || resultSet.RowSet.Count == 0)
        {
            _logger.LogWarning("No standings data returned for {Season}", seasonStr);
            return;
        }

        var idx = HeaderIndex(resultSet.Headers);

        // Ensure season entity exists
        var season = await _db.Seasons.FirstOrDefaultAsync(s => s.Year == seasonYear, ct);
        if (season == null)
        {
            season = new Season { Year = seasonYear };
            _db.Seasons.Add(season);
            await _db.SaveChangesAsync(ct);
        }

        int synced = 0;
        foreach (var row in resultSet.RowSet)
        {
            ct.ThrowIfCancellationRequested();

            var teamId = row[idx["TeamID"]].GetInt32();
            var conference = Str(row, idx, "Conference");
            var division = idx.ContainsKey("Division") ? Str(row, idx, "Division") : "";
            var teamCity = Str(row, idx, "TeamCity");
            var teamName = Str(row, idx, "TeamName");
            var teamAbbr = Str(row, idx, "TeamAbbreviation");
            var team = await UpsertTeamFromStandingsAsync(
                teamId, teamCity, teamName, teamAbbr, conference, division, ct);

            var snapshot = await _db.StandingsSnapshots
                .FirstOrDefaultAsync(s =>
                    s.TeamId == teamId && s.SeasonId == season.Id
                    && s.SnapshotDate == today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), ct);

            var wins = Int(row, idx, "WINS");
            var losses = Int(row, idx, "LOSSES");
            var winPct = Dec(row, idx, "WinPCT");
            var confRank = Int(row, idx, "PlayoffRank");
            var divRank = idx.ContainsKey("DivisionRank") ? Int(row, idx, "DivisionRank") : 0;
            var homeRecord = Str(row, idx, "HOME");
            var awayRecord = Str(row, idx, "ROAD");
            var last10 = Str(row, idx, "L10");
            var streak = Str(row, idx, "strCurrentStreak");
            var offRating = DecAny(row, idx, "E_OFF_RATING", "OffRating");
            var defRating = DecAny(row, idx, "E_DEF_RATING", "DefRating");
            var netRating = DecAny(row, idx, "E_NET_RATING", "NetRating");
            var pace = DecAny(row, idx, "E_PACE", "Pace");

            if (snapshot == null)
            {
                _db.StandingsSnapshots.Add(new StandingsSnapshot
                {
                    TeamId = teamId,
                    SeasonId = season.Id,
                    SnapshotDate = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                    Wins = wins,
                    Losses = losses,
                    WinPct = winPct,
                    ConfRank = confRank,
                    DivRank = divRank,
                    HomeRecord = homeRecord,
                    AwayRecord = awayRecord,
                    Last10 = last10,
                    Streak = streak,
                    OffRating = offRating,
                    DefRating = defRating,
                    NetRating = netRating,
                    Pace = pace,
                });
            }
            else
            {
                snapshot.Wins = wins;
                snapshot.Losses = losses;
                snapshot.WinPct = winPct;
                snapshot.ConfRank = confRank;
                snapshot.DivRank = divRank;
                snapshot.HomeRecord = homeRecord;
                snapshot.AwayRecord = awayRecord;
                snapshot.Last10 = last10;
                snapshot.Streak = streak;
                snapshot.OffRating = offRating;
                snapshot.DefRating = defRating;
                snapshot.NetRating = netRating;
                snapshot.Pace = pace;
            }

            synced++;
        }

        // Mark as synced
        _db.SyncStates.Add(new SyncState
        {
            Key = cursorKey,
            Value = "done",
            UpdatedAt = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Standings synced: {Count} teams for {Season} on {Date}",
            synced, seasonStr, today);
    }

    private static int Int(List<System.Text.Json.JsonElement> row,
        Dictionary<string, int> idx, string col)
    {
        if (!idx.TryGetValue(col, out var i)) return 0;
        var el = row[i];
        return el.ValueKind switch
        {
            System.Text.Json.JsonValueKind.Number => el.GetInt32(),
            System.Text.Json.JsonValueKind.Null => 0,
            _ => int.TryParse(el.GetString(), out var v) ? v : 0,
        };
    }

    private static decimal Dec(List<System.Text.Json.JsonElement> row,
        Dictionary<string, int> idx, string col)
    {
        if (!idx.TryGetValue(col, out var i)) return 0m;
        var el = row[i];
        return el.ValueKind switch
        {
            System.Text.Json.JsonValueKind.Number => el.GetDecimal(),
            System.Text.Json.JsonValueKind.Null => 0m,
            _ => decimal.TryParse(el.GetString(), out var v) ? v : 0m,
        };
    }

    private static string Str(List<System.Text.Json.JsonElement> row,
        Dictionary<string, int> idx, string col)
    {
        if (!idx.TryGetValue(col, out var i)) return string.Empty;
        var el = row[i];
        return el.ValueKind == System.Text.Json.JsonValueKind.Null
            ? string.Empty
            : el.GetString() ?? string.Empty;
    }

    private static decimal DecAny(List<System.Text.Json.JsonElement> row,
        Dictionary<string, int> idx, params string[] cols)
    {
        foreach (var col in cols)
        {
            if (idx.ContainsKey(col))
                return Dec(row, idx, col);
        }
        return 0m;
    }

    private static Dictionary<string, int> HeaderIndex(List<string> headers)
    {
        var d = new Dictionary<string, int>();
        for (int i = 0; i < headers.Count; i++)
            d[headers[i]] = i;
        return d;
    }

    private async Task<Team> UpsertTeamFromStandingsAsync(
        int teamId,
        string city,
        string name,
        string abbreviation,
        string conference,
        string division,
        CancellationToken ct)
    {
        city ??= string.Empty;
        name ??= string.Empty;
        abbreviation ??= string.Empty;
        conference ??= string.Empty;
        division ??= string.Empty;

        var team = await _db.Teams.FindAsync([teamId], ct);
        if (team == null)
        {
            team = new Team
            {
                Id = teamId,
                City = city,
                Name = name,
                FullName = $"{city} {name}".Trim(),
                Abbreviation = abbreviation,
                Conference = conference,
                Division = division,
                UpdatedAt = DateTime.UtcNow,
            };
            _db.Teams.Add(team);
        }
        else
        {
            if (!string.IsNullOrEmpty(city)) team.City = city;
            if (!string.IsNullOrEmpty(name))
            {
                team.Name = name;
                team.FullName = $"{team.City} {team.Name}".Trim();
            }
            if (!string.IsNullOrEmpty(abbreviation)) team.Abbreviation = abbreviation;
            if (!string.IsNullOrEmpty(conference)) team.Conference = conference;
            if (!string.IsNullOrEmpty(division)) team.Division = division;
            team.UpdatedAt = DateTime.UtcNow;
        }

        return team;
    }
}
