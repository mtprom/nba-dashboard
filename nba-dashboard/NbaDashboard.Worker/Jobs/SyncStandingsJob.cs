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
    private static readonly Dictionary<string, string> TeamAdvancedBaseParams = new()
    {
        ["College"] = "",
        ["Conference"] = "",
        ["Country"] = "",
        ["DateFrom"] = "",
        ["DateTo"] = "",
        ["Division"] = "",
        ["DraftPick"] = "",
        ["DraftYear"] = "",
        ["GameScope"] = "",
        ["GameSegment"] = "",
        ["Height"] = "",
        ["LastNGames"] = "0",
        ["LeagueID"] = "00",
        ["Location"] = "",
        ["MeasureType"] = "Advanced",
        ["Month"] = "0",
        ["OpponentTeamID"] = "0",
        ["Outcome"] = "",
        ["PORound"] = "0",
        ["PaceAdjust"] = "N",
        ["PerMode"] = "PerGame",
        ["Period"] = "0",
        ["PlayerExperience"] = "",
        ["PlayerPosition"] = "",
        ["PlusMinus"] = "N",
        ["Rank"] = "N",
        ["SeasonSegment"] = "",
        ["SeasonType"] = "Regular Season",
        ["ShotClockRange"] = "",
        ["StarterBench"] = "",
        ["TeamID"] = "0",
        ["TwoWay"] = "0",
        ["VsConference"] = "",
        ["VsDivision"] = "",
        ["Weight"] = "",
    };

    private readonly NbaStatsClient _nba;
    private readonly AppDbContext _db;
    private readonly ILogger<SyncStandingsJob> _logger;
    private readonly int _startSeasonYear;

    public SyncStandingsJob(
        NbaStatsClient nba,
        AppDbContext db,
        ILogger<SyncStandingsJob> logger,
        IConfiguration config)
    {
        _nba = nba;
        _db = db;
        _logger = logger;

        var startDate = DateOnly.Parse(config["NbaStats:BackfillStartDate"] ?? "2024-10-22");
        _startSeasonYear = startDate.Month >= 10 ? startDate.Year : startDate.Year - 1;
    }

    /// <summary>
    /// Sync current season standings. Used by nightly cron.
    /// </summary>
    public async Task RunAsync(CancellationToken ct = default)
    {
        int currentSeasonYear = GetCurrentSeasonYear();
        await SyncSeasonAsync(currentSeasonYear, currentSeasonYear, ct);
    }

    /// <summary>
    /// Replay standings for the full configured historical range. Used on startup
    /// so historical season comparisons have real team advanced metrics.
    /// </summary>
    public async Task RunBackfillRangeAsync(CancellationToken ct = default)
    {
        int currentSeasonYear = GetCurrentSeasonYear();

        for (int seasonYear = _startSeasonYear; seasonYear <= currentSeasonYear; seasonYear++)
        {
            ct.ThrowIfCancellationRequested();
            await SyncSeasonAsync(seasonYear, currentSeasonYear, ct);
        }
    }

    private async Task SyncSeasonAsync(int seasonYear, int currentSeasonYear, CancellationToken ct)
    {
        var seasonStr = $"{seasonYear}-{(seasonYear + 1) % 100:D2}";
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var isCompletedSeason = seasonYear < currentSeasonYear;

        var season = await _db.Seasons.FirstOrDefaultAsync(s => s.Year == seasonYear, ct);
        if (season == null)
        {
            season = new Season { Year = seasonYear };
            _db.Seasons.Add(season);
            await _db.SaveChangesAsync(ct);
        }

        var cursorKey = isCompletedSeason
            ? $"standings_complete_{seasonYear}"
            : $"standings_{seasonStr}_{today:yyyy-MM-dd}";
        var latestSnapshotIsStale = await LatestSnapshotHasAllZeroAdvancedMetricsAsync(season.Id, ct);

        if (!latestSnapshotIsStale && await _db.SyncStates.AnyAsync(s => s.Key == cursorKey, ct))
        {
            _logger.LogInformation("Standings for {Season} already synced with key {Key}, skipping",
                seasonStr, cursorKey);
            return;
        }

        if (latestSnapshotIsStale)
        {
            _logger.LogWarning(
                "Latest standings snapshot for {Season} has all-zero advanced metrics; replaying despite sync state",
                seasonStr);
        }

        _logger.LogInformation("Fetching standings and team advanced stats for {Season}", seasonStr);

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

        var advancedResp = await _nba.GetAsync<LeagueDashTeamStatsResponse>(
            "leaguedashteamstats",
            BuildTeamAdvancedParams(seasonStr),
            ct);
        var advancedSet = advancedResp?.ResultSets?.FirstOrDefault();
        if (!TryBuildAdvancedLookup(advancedSet, seasonStr, out var advancedLookup))
        {
            return;
        }

        var idx = HeaderIndex(resultSet.Headers);
        var standingsTeamIds = resultSet.RowSet
            .Select(row => Int(row, idx, "TeamID"))
            .Where(teamId => teamId != 0)
            .Distinct()
            .ToList();

        var missingTeamIds = standingsTeamIds
            .Where(teamId => !advancedLookup.ContainsKey(teamId))
            .ToList();
        if (missingTeamIds.Count > 0)
        {
            _logger.LogWarning(
                "Advanced team stats missing for {Season}; team ids without advanced rows: {TeamIds}",
                seasonStr, string.Join(", ", missingTeamIds));
            return;
        }

        var snapshotDate = await ResolveSnapshotDateAsync(season.Id, seasonYear, currentSeasonYear, today, ct);
        int synced = 0;
        foreach (var row in resultSet.RowSet)
        {
            ct.ThrowIfCancellationRequested();

            var teamId = row[idx["TeamID"]].GetInt32();
            var advanced = advancedLookup[teamId];
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
                    && s.SnapshotDate == snapshotDate, ct);

            var wins = Int(row, idx, "WINS");
            var losses = Int(row, idx, "LOSSES");
            var winPct = Dec(row, idx, "WinPCT");
            var confRank = Int(row, idx, "PlayoffRank");
            var divRank = idx.ContainsKey("DivisionRank") ? Int(row, idx, "DivisionRank") : 0;
            var homeRecord = Str(row, idx, "HOME");
            var awayRecord = Str(row, idx, "ROAD");
            var last10 = Str(row, idx, "L10");
            var streak = Str(row, idx, "strCurrentStreak");

            if (snapshot == null)
            {
                _db.StandingsSnapshots.Add(new StandingsSnapshot
                {
                    TeamId = teamId,
                    SeasonId = season.Id,
                    SnapshotDate = snapshotDate,
                    Wins = wins,
                    Losses = losses,
                    WinPct = winPct,
                    ConfRank = confRank,
                    DivRank = divRank,
                    HomeRecord = homeRecord,
                    AwayRecord = awayRecord,
                    Last10 = last10,
                    Streak = streak,
                    OffRating = advanced.OffRating,
                    DefRating = advanced.DefRating,
                    NetRating = advanced.NetRating,
                    Pace = advanced.Pace,
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
                snapshot.OffRating = advanced.OffRating;
                snapshot.DefRating = advanced.DefRating;
                snapshot.NetRating = advanced.NetRating;
                snapshot.Pace = advanced.Pace;
            }

            synced++;
        }

        UpsertSyncState(cursorKey, isCompletedSeason ? "complete" : "done");

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Standings synced: {Count} teams for {Season} on {Date}",
            synced, seasonStr, snapshotDate);
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

    private bool TryBuildAdvancedLookup(
        TeamStatsResultSet? resultSet,
        string seasonStr,
        out Dictionary<int, TeamAdvancedMetrics> lookup)
    {
        lookup = new Dictionary<int, TeamAdvancedMetrics>();

        if (resultSet == null || resultSet.RowSet.Count == 0)
        {
            _logger.LogWarning("No advanced team stats returned for {Season}", seasonStr);
            return false;
        }

        var idx = HeaderIndex(resultSet.Headers);
        string[] requiredHeaders = ["TEAM_ID", "OFF_RATING", "DEF_RATING", "NET_RATING", "PACE"];
        var missingHeaders = requiredHeaders.Where(header => !idx.ContainsKey(header)).ToList();
        if (missingHeaders.Count > 0)
        {
            _logger.LogWarning(
                "Advanced team stats for {Season} are missing required headers {Headers}. Returned headers: {ReturnedHeaders}",
                seasonStr,
                string.Join(", ", missingHeaders),
                string.Join(", ", resultSet.Headers));
            return false;
        }

        foreach (var row in resultSet.RowSet)
        {
            var teamId = Int(row, idx, "TEAM_ID");
            if (teamId == 0)
            {
                continue;
            }

            lookup[teamId] = new TeamAdvancedMetrics
            {
                OffRating = Dec(row, idx, "OFF_RATING"),
                DefRating = Dec(row, idx, "DEF_RATING"),
                NetRating = Dec(row, idx, "NET_RATING"),
                Pace = Dec(row, idx, "PACE"),
            };
        }

        if (lookup.Count == 0)
        {
            _logger.LogWarning("Advanced team stats returned zero valid team rows for {Season}", seasonStr);
            return false;
        }

        return true;
    }

    private static Dictionary<string, int> HeaderIndex(List<string> headers)
    {
        var d = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Count; i++)
            d[headers[i]] = i;
        return d;
    }

    private static Dictionary<string, string> BuildTeamAdvancedParams(string seasonStr)
    {
        var parameters = new Dictionary<string, string>(TeamAdvancedBaseParams)
        {
            ["Season"] = seasonStr,
        };
        return parameters;
    }

    private async Task<bool> LatestSnapshotHasAllZeroAdvancedMetricsAsync(int seasonId, CancellationToken ct)
    {
        var latestDate = await _db.StandingsSnapshots
            .Where(s => s.SeasonId == seasonId)
            .MaxAsync(s => (DateTime?)s.SnapshotDate, ct);

        if (latestDate == null)
        {
            return false;
        }

        var latestSnapshots = await _db.StandingsSnapshots
            .Where(s => s.SeasonId == seasonId && s.SnapshotDate == latestDate.Value)
            .Select(s => new { s.OffRating, s.DefRating, s.NetRating, s.Pace })
            .ToListAsync(ct);

        return latestSnapshots.Count > 0
            && latestSnapshots.All(s =>
                s.OffRating == 0m && s.DefRating == 0m && s.NetRating == 0m && s.Pace == 0m);
    }

    private async Task<DateTime> ResolveSnapshotDateAsync(
        int seasonId,
        int seasonYear,
        int currentSeasonYear,
        DateOnly today,
        CancellationToken ct)
    {
        if (seasonYear == currentSeasonYear)
        {
            return today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        }

        var latestDate = await _db.StandingsSnapshots
            .Where(s => s.SeasonId == seasonId)
            .MaxAsync(s => (DateTime?)s.SnapshotDate, ct);

        return latestDate ?? new DateTime(seasonYear + 1, 6, 30, 0, 0, 0, DateTimeKind.Utc);
    }

    private void UpsertSyncState(string key, string value)
    {
        var existing = _db.SyncStates.Local.FirstOrDefault(s => s.Key == key)
            ?? _db.SyncStates.FirstOrDefault(s => s.Key == key);

        if (existing == null)
        {
            _db.SyncStates.Add(new SyncState
            {
                Key = key,
                Value = value,
                UpdatedAt = DateTime.UtcNow,
            });
            return;
        }

        existing.Value = value;
        existing.UpdatedAt = DateTime.UtcNow;
    }

    private static int GetCurrentSeasonYear()
    {
        var now = DateTime.UtcNow;
        return now.Month >= 10 ? now.Year : now.Year - 1;
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

    private sealed class TeamAdvancedMetrics
    {
        public decimal OffRating { get; init; }
        public decimal DefRating { get; init; }
        public decimal NetRating { get; init; }
        public decimal Pace { get; init; }
    }
}
