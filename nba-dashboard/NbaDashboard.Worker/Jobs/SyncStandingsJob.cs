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
    /// Sync current season standings. Used by both startup and nightly cron.
    /// </summary>
    public async Task RunAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        int seasonYear = now.Month >= 10 ? now.Year : now.Year - 1;
        var seasonStr = $"{seasonYear}-{(seasonYear + 1) % 100:D2}";
        var today = DateOnly.FromDateTime(now);

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

            // Skip if team doesn't exist in our DB
            var team = await _db.Teams.FindAsync([teamId], ct);
            if (team == null)
            {
                _logger.LogDebug("Team {TeamId} not in DB, skipping standings row", teamId);
                continue;
            }

            // Always update team identity from standings (canonical source for current names)
            var conference = Str(row, idx, "Conference");
            var division = idx.ContainsKey("Division") ? Str(row, idx, "Division") : "";
            var teamCity = Str(row, idx, "TeamCity");
            var teamName = Str(row, idx, "TeamName");
            var teamAbbr = Str(row, idx, "TeamAbbreviation");
            if (!string.IsNullOrEmpty(conference))
            {
                team.Conference = conference;
                team.Division = division;
            }
            if (!string.IsNullOrEmpty(teamName))
            {
                team.Name = teamName;
                team.FullName = $"{teamCity} {teamName}".Trim();
                if (!string.IsNullOrEmpty(teamCity)) team.City = teamCity;
                if (!string.IsNullOrEmpty(teamAbbr)) team.Abbreviation = teamAbbr;
            }
            team.UpdatedAt = DateTime.UtcNow;

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
            var offRating = Dec(row, idx, "OffRating");
            var defRating = Dec(row, idx, "DefRating");
            var netRating = Dec(row, idx, "NetRating");
            var pace = Dec(row, idx, "Pace");

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

    private static Dictionary<string, int> HeaderIndex(List<string> headers)
    {
        var d = new Dictionary<string, int>();
        for (int i = 0; i < headers.Count; i++)
            d[headers[i]] = i;
        return d;
    }
}
