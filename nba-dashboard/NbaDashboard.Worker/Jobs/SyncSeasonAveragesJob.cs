using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NbaDashboard.Core.Entities;
using NbaDashboard.Infrastructure.Data;
using NbaDashboard.Infrastructure.NbaStats;
using NbaDashboard.Infrastructure.NbaStats.Models;

namespace NbaDashboard.Worker.Jobs;

public class SyncSeasonAveragesJob
{
    private readonly NbaStatsClient _nba;
    private readonly AppDbContext _db;
    private readonly ILogger<SyncSeasonAveragesJob> _logger;
    private readonly int _startSeasonYear;

    // Required query params for leaguedashplayerstats (empty strings = "all")
    private static readonly Dictionary<string, string> BaseParams = new()
    {
        ["LeagueID"]         = "00",
        ["SeasonType"]       = "Regular Season",
        ["PerMode"]          = "PerGame",
        ["DateFrom"]         = "",
        ["DateTo"]           = "",
        ["GameScope"]        = "",
        ["GameSegment"]      = "",
        ["LastNGames"]       = "0",
        ["Location"]         = "",
        ["Month"]            = "0",
        ["OpponentTeamID"]   = "0",
        ["Outcome"]          = "",
        ["PORound"]          = "0",
        ["PaceAdjust"]       = "N",
        ["Period"]           = "0",
        ["PlayerExperience"] = "",
        ["PlayerPosition"]   = "",
        ["PlusMinus"]        = "N",
        ["Rank"]             = "N",
        ["SeasonSegment"]    = "",
        ["ShotClockRange"]   = "",
        ["StarterBench"]     = "",
        ["TeamID"]           = "0",
        ["TwoWay"]           = "0",
        ["VsConference"]     = "",
        ["VsDivision"]       = "",
        ["Weight"]           = "",
    };

    public SyncSeasonAveragesJob(NbaStatsClient nba, AppDbContext db,
        ILogger<SyncSeasonAveragesJob> logger, IConfiguration config)
    {
        _nba = nba;
        _db = db;
        _logger = logger;

        var startDate = DateOnly.Parse(config["NbaStats:BackfillStartDate"] ?? "2024-10-22");
        _startSeasonYear = startDate.Month >= 10 ? startDate.Year : startDate.Year - 1;
    }

    /// <summary>
    /// Backfill all seasons from start through current. Used on startup.
    /// </summary>
    public async Task RunAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        int currentSeasonYear = now.Month >= 10 ? now.Year : now.Year - 1;

        _logger.LogInformation("Starting season averages backfill: {Start} to {End}",
            SeasonString(_startSeasonYear), SeasonString(currentSeasonYear));

        for (int year = _startSeasonYear; year <= currentSeasonYear; year++)
        {
            ct.ThrowIfCancellationRequested();

            var cursorKey = $"season_avg_{year}";
            if (await _db.SyncStates.AnyAsync(s => s.Key == cursorKey, ct))
            {
                _logger.LogInformation("Season averages for {Season} already synced, skipping",
                    SeasonString(year));
                continue;
            }

            await SyncSeasonAsync(year, ct);

            // Mark past seasons complete (not the current one — stats still updating)
            if (year < currentSeasonYear)
            {
                _db.SyncStates.Add(new SyncState
                {
                    Key       = cursorKey,
                    Value     = "done",
                    UpdatedAt = DateTime.UtcNow,
                });
                await _db.SaveChangesAsync(ct);
            }
        }

        _logger.LogInformation("Season averages backfill complete");
    }

    /// <summary>
    /// Sync current season only. Used by the nightly cron job.
    /// </summary>
    public async Task SyncCurrentSeasonAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        int currentSeasonYear = now.Month >= 10 ? now.Year : now.Year - 1;
        await SyncSeasonAsync(currentSeasonYear, ct);
    }

    private async Task SyncSeasonAsync(int year, CancellationToken ct)
    {
        var seasonStr = SeasonString(year);
        _logger.LogInformation("Syncing season averages for {Season}", seasonStr);

        // Ensure season entity exists
        var season = await _db.Seasons.FirstOrDefaultAsync(s => s.Year == year, ct);
        if (season == null)
        {
            season = new Season { Year = year };
            _db.Seasons.Add(season);
            await _db.SaveChangesAsync(ct);
        }

        // --- Call 1: Traditional stats ---
        var tradParams = new Dictionary<string, string>(BaseParams)
        {
            ["Season"]      = seasonStr,
            ["MeasureType"] = "Base",
        };

        var tradResp = await _nba.GetAsync<LeagueDashPlayerStatsResponse>(
            "leaguedashplayerstats", tradParams, ct);

        var tradSet = tradResp?.ResultSets?.FirstOrDefault();
        if (tradSet == null || tradSet.RowSet.Count == 0)
        {
            _logger.LogWarning("No traditional season stats returned for {Season}", seasonStr);
            return;
        }

        var tIdx = HeaderIndex(tradSet.Headers);

        // Build lookup: (playerId, teamId) → row
        var tradDict = new Dictionary<(int, int), List<System.Text.Json.JsonElement>>();
        foreach (var row in tradSet.RowSet)
        {
            var pid = row[tIdx["PLAYER_ID"]].GetInt32();
            var tid = row[tIdx["TEAM_ID"]].GetInt32();
            tradDict[(pid, tid)] = row;
        }

        _logger.LogInformation("Traditional stats: {Count} player-team rows for {Season}",
            tradDict.Count, seasonStr);

        // --- Call 2: Advanced stats ---
        var advParams = new Dictionary<string, string>(BaseParams)
        {
            ["Season"]      = seasonStr,
            ["MeasureType"] = "Advanced",
        };

        var advResp = await _nba.GetAsync<LeagueDashPlayerStatsResponse>(
            "leaguedashplayerstats", advParams, ct);

        var advSet = advResp?.ResultSets?.FirstOrDefault();
        var aIdx = advSet != null ? HeaderIndex(advSet.Headers) : [];

        var advDict = new Dictionary<(int, int), List<System.Text.Json.JsonElement>>();
        if (advSet != null)
        {
            foreach (var row in advSet.RowSet)
            {
                var pid = row[aIdx["PLAYER_ID"]].GetInt32();
                var tid = row[aIdx["TEAM_ID"]].GetInt32();
                advDict[(pid, tid)] = row;
            }
        }

        // --- First pass ---
        var skipped = new List<(int playerId, int teamId)>();
        int synced = 0;

        foreach (var ((playerId, teamId), tradRow) in tradDict)
        {
            ct.ThrowIfCancellationRequested();

            if (!await TryUpsertAsync(playerId, teamId, season.Id, tradRow, tIdx, advDict, aIdx, ct))
                skipped.Add((playerId, teamId));
            else
                synced++;
        }

        // --- Second pass: retry skipped ---
        foreach (var (playerId, teamId) in skipped)
        {
            ct.ThrowIfCancellationRequested();

            if (tradDict.TryGetValue((playerId, teamId), out var tradRow))
            {
                if (await TryUpsertAsync(playerId, teamId, season.Id, tradRow, tIdx, advDict, aIdx, ct))
                    synced++;
                else
                    _logger.LogWarning(
                        "Skipping player {PlayerId} team {TeamId} for {Season} — FK dependency missing",
                        playerId, teamId, seasonStr);
            }
        }

        _logger.LogInformation("Season averages for {Season}: {Synced}/{Total} synced",
            seasonStr, synced, tradDict.Count);
    }

    /// <summary>
    /// Returns true if upsert succeeded, false if a FK dependency (Player/Team) is missing.
    /// </summary>
    private async Task<bool> TryUpsertAsync(
        int playerId, int teamId, int seasonId,
        List<System.Text.Json.JsonElement> tradRow,
        Dictionary<string, int> tIdx,
        Dictionary<(int, int), List<System.Text.Json.JsonElement>> advDict,
        Dictionary<string, int> aIdx,
        CancellationToken ct)
    {
        if (await _db.Players.FindAsync([playerId], ct) == null) return false;
        if (await _db.Teams.FindAsync([teamId], ct) == null)     return false;

        advDict.TryGetValue((playerId, teamId), out var advRow);

        var gp     = tradRow[tIdx["GP"]].GetInt32();
        var minAvg = Dec(tradRow, tIdx, "MIN");
        var ptsAvg = Dec(tradRow, tIdx, "PTS");
        var rebAvg = Dec(tradRow, tIdx, "REB");
        var astAvg = Dec(tradRow, tIdx, "AST");
        var stlAvg = Dec(tradRow, tIdx, "STL");
        var blkAvg = Dec(tradRow, tIdx, "BLK");
        var toAvg  = Dec(tradRow, tIdx, "TOV");
        var fgPct  = Dec(tradRow, tIdx, "FG_PCT");
        var fg3Pct = Dec(tradRow, tIdx, "FG3_PCT");
        var ftPct  = Dec(tradRow, tIdx, "FT_PCT");

        var tsPct     = advRow != null ? Dec(advRow, aIdx, "TS_PCT")    : 0m;
        var usgPct    = advRow != null ? Dec(advRow, aIdx, "USG_PCT")   : 0m;
        var netRating = advRow != null ? Dec(advRow, aIdx, "NET_RATING"): 0m;
        var pie       = advRow != null ? Dec(advRow, aIdx, "PIE")       : 0m;

        var existing = await _db.PlayerSeasonStats
            .FirstOrDefaultAsync(s =>
                s.PlayerId == playerId && s.SeasonId == seasonId && s.TeamId == teamId, ct);

        if (existing == null)
        {
            _db.PlayerSeasonStats.Add(new PlayerSeasonStats
            {
                PlayerId    = playerId,
                SeasonId    = seasonId,
                TeamId      = teamId,
                GamesPlayed = gp,
                MinAvg      = minAvg,
                PtsAvg      = ptsAvg,
                RebAvg      = rebAvg,
                AstAvg      = astAvg,
                StlAvg      = stlAvg,
                BlkAvg      = blkAvg,
                ToAvg       = toAvg,
                FgPct       = fgPct,
                Fg3Pct      = fg3Pct,
                FtPct       = ftPct,
                TsPct       = tsPct,
                UsgPct      = usgPct,
                NetRating   = netRating,
                Pie         = pie,
                Per         = 0m, // not available from leaguedashplayerstats
                UpdatedAt   = DateTime.UtcNow,
            });
        }
        else
        {
            existing.GamesPlayed = gp;
            existing.MinAvg      = minAvg;
            existing.PtsAvg      = ptsAvg;
            existing.RebAvg      = rebAvg;
            existing.AstAvg      = astAvg;
            existing.StlAvg      = stlAvg;
            existing.BlkAvg      = blkAvg;
            existing.ToAvg       = toAvg;
            existing.FgPct       = fgPct;
            existing.Fg3Pct      = fg3Pct;
            existing.FtPct       = ftPct;
            existing.TsPct       = tsPct;
            existing.UsgPct      = usgPct;
            existing.NetRating   = netRating;
            existing.Pie         = pie;
            existing.UpdatedAt   = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static decimal Dec(List<System.Text.Json.JsonElement> row,
        Dictionary<string, int> idx, string col)
    {
        var el = row[idx[col]];
        return el.ValueKind switch
        {
            System.Text.Json.JsonValueKind.Number => el.GetDecimal(),
            System.Text.Json.JsonValueKind.Null   => 0m,
            _ => decimal.TryParse(el.GetString(), out var v) ? v : 0m,
        };
    }

    private static Dictionary<string, int> HeaderIndex(List<string> headers)
    {
        var d = new Dictionary<string, int>();
        for (int i = 0; i < headers.Count; i++)
            d[headers[i]] = i;
        return d;
    }

    private static string SeasonString(int year) =>
        $"{year}-{(year + 1) % 100:D2}";
}
