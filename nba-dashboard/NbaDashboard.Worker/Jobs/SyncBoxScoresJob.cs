using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NbaDashboard.Core.Entities;
using NbaDashboard.Infrastructure.Data;
using NbaDashboard.Infrastructure.NbaStats;
using NbaDashboard.Infrastructure.NbaStats.Models;

namespace NbaDashboard.Worker.Jobs;

public class SyncBoxScoresJob
{
    private const int RollingRecentDays = 3;
    private readonly NbaStatsClient _nba;
    private readonly AppDbContext _db;
    private readonly ILogger<SyncBoxScoresJob> _logger;

    public SyncBoxScoresJob(NbaStatsClient nba, AppDbContext db, ILogger<SyncBoxScoresJob> logger)
    {
        _nba = nba;
        _db = db;
        _logger = logger;
    }

    public async Task RunAsync(DateOnly? targetDate = null, CancellationToken ct = default)
    {
        if (targetDate.HasValue)
        {
            await RunForDateAsync(targetDate.Value, ct);
            return;
        }

        var dates = Enumerable.Range(1, RollingRecentDays)
            .Select(offset => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-offset)))
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        _logger.LogInformation("SyncBoxScoresJob starting rolling sweep for {Count} completed dates", dates.Count);

        foreach (var date in dates)
        {
            ct.ThrowIfCancellationRequested();
            await RunForDateAsync(date, ct);
        }

        _logger.LogInformation("SyncBoxScoresJob complete for rolling sweep");
    }

    public async Task<SeasonCoverageAudit> RunForSeasonAsync(int seasonStartYear, CancellationToken ct = default)
    {
        var seasonStr = $"{seasonStartYear}-{(seasonStartYear + 1) % 100:D2}";
        _logger.LogInformation("Fetching all games for season {Season}", seasonStr);

        var games = await GetSeasonGamesAsync(seasonStartYear, ct);
        if (games.Count == 0)
        {
            _logger.LogInformation("No games found for season {Season}", seasonStr);
            return new SeasonCoverageAudit(seasonStr, 0, 0, 0, 0, 0);
        }

        _logger.LogInformation("Found {Count} unique games for season {Season}", games.Count, seasonStr);

        var season = await UpsertSeasonAsync(seasonStartYear, ct);
        int processed = 0;

        foreach (var (gameId, date) in games)
        {
            ct.ThrowIfCancellationRequested();

            if (await IsGameAlreadyCompleteAsync(gameId, ct))
            {
                continue;
            }

            var result = await ProcessGameAsync(gameId, season, date, ct);
            if (result.IsComplete)
                processed++;

            _logger.LogInformation("Processed {GameId} ({Processed}/{Total}) status={Status}",
                gameId, processed, games.Count, result.Status);
        }

        var audit = await AuditSeasonCoverageAsync(seasonStr, games.Select(g => g.GameId).ToList(), ct);
        _logger.LogInformation(
            "Season {Season} coverage audit: complete={Complete} incomplete={Incomplete} missing={Missing} orphanedSync={OrphanedSync}/{Expected}",
            seasonStr, audit.CompleteGames, audit.IncompleteGames, audit.MissingGames, audit.OrphanedSyncStates, audit.ExpectedGames);
        return audit;
    }

    private async Task<Season> UpsertSeasonAsync(int year, CancellationToken ct)
    {
        var season = await _db.Seasons.FirstOrDefaultAsync(s => s.Year == year, ct);
        if (season == null)
        {
            season = new Season { Year = year };
            _db.Seasons.Add(season);
            await _db.SaveChangesAsync(ct);
        }
        return season;
    }

    private async Task UpsertTeamAsync(int id, string city, string name, string tricode, CancellationToken ct)
    {
        city ??= "";
        name ??= "";
        tricode ??= "";

        var team = await _db.Teams.FindAsync([id], ct);
        if (team == null)
        {
            _db.Teams.Add(new Team
            {
                Id           = id,
                City         = city,
                Name         = name,
                FullName     = $"{city} {name}".Trim(),
                Abbreviation = tricode,
                UpdatedAt    = DateTime.UtcNow,
            });
        }
        else
        {
            // Only update name/city if standings hasn't set the canonical identity yet
            if (string.IsNullOrEmpty(team.Conference))
            {
                if (!string.IsNullOrEmpty(tricode)) team.Abbreviation = tricode;
                if (!string.IsNullOrEmpty(name)) { team.Name = name; team.FullName = $"{city} {name}".Trim(); }
                if (!string.IsNullOrEmpty(city)) team.City = city;
            }
            team.UpdatedAt    = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
    }

    private async Task UpsertPlayerAsync(int id, string firstName, string familyName,
        string position, string jerseyNum, int teamId, CancellationToken ct)
    {
        firstName ??= "";
        familyName ??= "";
        position ??= "";
        jerseyNum ??= "";

        var player = await _db.Players.FindAsync([id], ct);
        if (player == null)
        {
            _db.Players.Add(new Player
            {
                Id           = id,
                FirstName    = firstName,
                LastName     = familyName,
                Position     = position,
                JerseyNumber = jerseyNum,
                TeamId       = teamId,
                IsActive     = true,
                UpdatedAt    = DateTime.UtcNow,
            });
        }
        else
        {
            player.TeamId       = teamId;
            if (!string.IsNullOrEmpty(position)) player.Position = position;
            if (!string.IsNullOrEmpty(jerseyNum)) player.JerseyNumber = jerseyNum;
            player.UpdatedAt    = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
    }

    private async Task RunForDateAsync(DateOnly date, CancellationToken ct)
    {
        var dateStr = date.ToString("yyyy-MM-dd");
        _logger.LogInformation("SyncBoxScoresJob starting for {Date}", dateStr);

        var games = await GetGamesForDateAsync(date, ct);
        if (games.Count == 0)
        {
            _logger.LogInformation("No games found for {Date}", dateStr);
            return;
        }

        _logger.LogInformation("Found {Count} games for {Date}", games.Count, dateStr);

        int completed = 0;
        int incomplete = 0;
        foreach (var (gameId, gameDate) in games)
        {
            ct.ThrowIfCancellationRequested();

            var season = await UpsertSeasonAsync(SeasonStartYearForDate(gameDate), ct);
            if (await IsGameAlreadyCompleteAsync(gameId, ct))
            {
                completed++;
                continue;
            }

            var result = await ProcessGameAsync(gameId, season, gameDate, ct);
            if (result.IsComplete)
                completed++;
            else
                incomplete++;
        }

        var audit = await AuditSeasonCoverageAsync(SeasonStringForDate(date), games.Select(g => g.GameId).ToList(), ct);
        _logger.LogInformation(
            "SyncBoxScoresJob complete for {Date}: complete={Complete} incomplete={Incomplete} missing={Missing} orphanedSync={OrphanedSync}",
            dateStr, audit.CompleteGames, incomplete, audit.MissingGames, audit.OrphanedSyncStates);
    }

    // Converts a date to NBA season string e.g. date in 2024-25 season → "2024-25"
    private static string SeasonStringForDate(DateOnly date)
    {
        // NBA season starts in October; if month < 7 we're still in prior year's season
        int startYear = SeasonStartYearForDate(date);
        return $"{startYear}-{(startYear + 1) % 100:D2}";
    }

    private static int SeasonStartYearForDate(DateOnly date)
        => date.Month >= 10 ? date.Year : date.Year - 1;

    private async Task<List<(string GameId, DateOnly Date)>> GetGamesForDateAsync(DateOnly date, CancellationToken ct)
    {
        var dateStr = date.ToString("yyyy-MM-dd");
        var finderResp = await _nba.GetAsync<LeagueGameFinderResponse>(
            "leaguegamefinder",
            new Dictionary<string, string>
            {
                ["LeagueID"] = "00",
                ["Season"] = SeasonStringForDate(date),
                ["SeasonType"] = "Regular Season",
                ["DateFrom"] = dateStr,
                ["DateTo"] = dateStr,
            },
            ct);

        var resultSet = finderResp?.ResultSets?.FirstOrDefault();
        if (resultSet == null || resultSet.RowSet.Count == 0)
            return [];

        return ParseGameRows(resultSet);
    }

    private async Task<List<(string GameId, DateOnly Date)>> GetSeasonGamesAsync(int seasonStartYear, CancellationToken ct)
    {
        var seasonStr = $"{seasonStartYear}-{(seasonStartYear + 1) % 100:D2}";
        var finderResp = await _nba.GetAsync<LeagueGameFinderResponse>(
            "leaguegamefinder",
            new Dictionary<string, string>
            {
                ["LeagueID"] = "00",
                ["Season"] = seasonStr,
                ["SeasonType"] = "Regular Season",
            },
            ct);

        var resultSet = finderResp?.ResultSets?.FirstOrDefault();
        if (resultSet == null || resultSet.RowSet.Count == 0)
            return [];

        return ParseGameRows(resultSet)
            .OrderByDescending(g => g.Date)
            .ToList();
    }

    private static List<(string GameId, DateOnly Date)> ParseGameRows(GameFinderResultSet resultSet)
    {
        var headers = resultSet.Headers;
        int gameIdIdx = headers.IndexOf("GAME_ID");
        int gameDateIdx = headers.IndexOf("GAME_DATE");

        return resultSet.RowSet
            .GroupBy(row => row[gameIdIdx].GetString()!)
            .Select(g => (
                GameId: g.Key,
                Date: DateOnly.Parse(g.First()[gameDateIdx].GetString()!)
            ))
            .ToList();
    }

    private async Task<GameSyncResult> ProcessGameAsync(string gameId, Season season, DateOnly date, CancellationToken ct)
    {
        try
        {
            var result = await SyncGameAsync(gameId, season, date, ct);
            var coverage = await GetCoverageStatusAsync(gameId, ct);

            if (result.IsSuccessful && coverage.IsComplete)
            {
                await MarkGameCompleteAsync(gameId, ct);
                return new GameSyncResult(true, "complete", null);
            }

            var reason = result.FailureReason ?? "coverage_incomplete";
            await MarkGameIncompleteAsync(gameId, reason, ct);
            return new GameSyncResult(false, "incomplete", reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync game {GameId}", gameId);
            await MarkGameIncompleteAsync(gameId, $"exception:{ex.GetType().Name}", ct);
            return new GameSyncResult(false, "failed", ex.Message);
        }
    }

    private async Task<GameSyncAttempt> SyncGameAsync(string gameId, Season season, DateOnly date, CancellationToken ct)
    {
        _logger.LogInformation("Syncing game {GameId}", gameId);

        var tradResp = await _nba.GetAsync<BoxScoreTraditionalV3Response>(
            "boxscoretraditionalv3",
            new Dictionary<string, string> { ["GameID"] = gameId },
            ct);

        var trad = tradResp?.BoxScoreTraditional;
        if (trad == null)
        {
            _logger.LogWarning("No traditional data for {GameId}", gameId);
            return new GameSyncAttempt(false, "missing_traditional");
        }

        await UpsertTeamAsync(trad.HomeTeam.TeamId, trad.HomeTeam.TeamCity,
            trad.HomeTeam.TeamName, trad.HomeTeam.TeamTricode, ct);
        await UpsertTeamAsync(trad.AwayTeam.TeamId, trad.AwayTeam.TeamCity,
            trad.AwayTeam.TeamName, trad.AwayTeam.TeamTricode, ct);

        foreach (var p in trad.HomeTeam.Players)
            await UpsertPlayerAsync(p.PersonId, p.FirstName, p.FamilyName,
                p.Position, p.JerseyNum, trad.HomeTeam.TeamId, ct);
        foreach (var p in trad.AwayTeam.Players)
            await UpsertPlayerAsync(p.PersonId, p.FirstName, p.FamilyName,
                p.Position, p.JerseyNum, trad.AwayTeam.TeamId, ct);

        var homeScore = trad.HomeTeam.Players.Sum(p => p.Statistics.Points);
        var awayScore = trad.AwayTeam.Players.Sum(p => p.Statistics.Points);

        var game = await _db.Games.FindAsync([gameId], ct);
        if (game == null)
        {
            game = new Game
            {
                Id = gameId,
                SeasonId = season.Id,
                Date = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                Status = "Final",
                HomeTeamId = trad.HomeTeamId,
                VisitorTeamId = trad.AwayTeamId,
                HomeScore = homeScore,
                VisitorScore = awayScore,
            };
            _db.Games.Add(game);
        }
        else
        {
            game.HomeScore = homeScore;
            game.VisitorScore = awayScore;
            game.Status = "Final";
        }
        await _db.SaveChangesAsync(ct);

        var allTradPlayers = trad.HomeTeam.Players
            .Select(p => (player: p, teamId: trad.HomeTeam.TeamId))
            .Concat(trad.AwayTeam.Players.Select(p => (player: p, teamId: trad.AwayTeam.TeamId)))
            .DistinctBy(x => x.player.PersonId);

        foreach (var (p, teamId) in allTradPlayers)
        {
            var s = p.Statistics;
            var existing = await _db.PlayerGameStats
                .FirstOrDefaultAsync(x => x.GameId == gameId && x.PlayerId == p.PersonId, ct);

            if (existing == null)
            {
                _db.PlayerGameStats.Add(new PlayerGameStats
                {
                    GameId = gameId,
                    PlayerId = p.PersonId,
                    TeamId = teamId,
                    StartPosition = p.Position,
                    Minutes = s.MinutesDecimal(),
                    Points = s.Points,
                    Rebounds = s.ReboundsTotal,
                    Assists = s.Assists,
                    Steals = s.Steals,
                    Blocks = s.Blocks,
                    Turnovers = s.Turnovers,
                    PersonalFouls = s.FoulsPersonal,
                    PlusMinus = (int)s.PlusMinusPoints,
                    FieldGoalsMade = s.FieldGoalsMade,
                    FieldGoalsAttempted = s.FieldGoalsAttempted,
                    FieldGoalPct = (decimal)s.FieldGoalsPercentage,
                    ThreePointersMade = s.ThreePointersMade,
                    ThreePointersAttempted = s.ThreePointersAttempted,
                    ThreePointPct = (decimal)s.ThreePointersPercentage,
                    FreeThrowsMade = s.FreeThrowsMade,
                    FreeThrowsAttempted = s.FreeThrowsAttempted,
                    FreeThrowPct = (decimal)s.FreeThrowsPercentage,
                    OffensiveRebounds = s.ReboundsOffensive,
                    DefensiveRebounds = s.ReboundsDefensive,
                });
            }
        }
        await _db.SaveChangesAsync(ct);

        var advResp = await _nba.GetAsync<BoxScoreAdvancedV3Response>(
            "boxscoreadvancedv3",
            new Dictionary<string, string> { ["GameID"] = gameId },
            ct);

        var adv = advResp?.BoxScoreAdvanced;
        if (adv == null)
        {
            _logger.LogWarning("No advanced data for {GameId}", gameId);
            return new GameSyncAttempt(false, "missing_advanced");
        }

        var allAdvPlayers = adv.HomeTeam.Players
            .Select(p => (player: p, teamId: adv.HomeTeam.TeamId))
            .Concat(adv.AwayTeam.Players.Select(p => (player: p, teamId: adv.AwayTeam.TeamId)))
            .DistinctBy(x => x.player.PersonId);

        foreach (var (p, teamId) in allAdvPlayers)
        {
            var s = p.Statistics;
            var existing = await _db.PlayerGameAdvanced
                .FirstOrDefaultAsync(x => x.GameId == gameId && x.PlayerId == p.PersonId, ct);

            if (existing == null)
            {
                _db.PlayerGameAdvanced.Add(new PlayerGameAdvanced
                {
                    GameId = gameId,
                    PlayerId = p.PersonId,
                    TeamId = teamId,
                    Minutes = s.MinutesDecimal(),
                    OffRating = (decimal)s.OffensiveRating,
                    DefRating = (decimal)s.DefensiveRating,
                    NetRating = (decimal)s.NetRating,
                    AstPct = (decimal)s.AssistPercentage,
                    OrebPct = (decimal)s.OffensiveReboundPercentage,
                    DrebPct = (decimal)s.DefensiveReboundPercentage,
                    RebPct = (decimal)s.ReboundPercentage,
                    EfgPct = (decimal)s.EffectiveFieldGoalPercentage,
                    TsPct = (decimal)s.TrueShootingPercentage,
                    UsgPct = (decimal)s.UsagePercentage,
                    Pace = (decimal)s.Pace,
                    Pie = (decimal)s.PIE,
                });
            }
        }
        await _db.SaveChangesAsync(ct);

        return new GameSyncAttempt(true, null);
    }

    private async Task<bool> IsGameAlreadyCompleteAsync(string gameId, CancellationToken ct)
    {
        var syncKey = BoxscoreSyncKey(gameId);
        if (!await _db.SyncStates.AnyAsync(s => s.Key == syncKey, ct))
            return false;

        var coverage = await GetCoverageStatusAsync(gameId, ct);
        if (coverage.IsComplete)
            return true;

        _logger.LogWarning("Game {GameId} has stale complete sync state but incomplete coverage; retrying", gameId);
        await MarkGameIncompleteAsync(gameId, "stale_complete_sync", ct);
        return false;
    }

    private async Task<GameCoverageStatus> GetCoverageStatusAsync(string gameId, CancellationToken ct)
    {
        var hasGame = await _db.Games.AnyAsync(g => g.Id == gameId, ct);
        if (!hasGame)
            return new GameCoverageStatus(false, 0, 0);

        var tradPlayers = await _db.PlayerGameStats
            .Where(s => s.GameId == gameId)
            .Select(s => s.PlayerId)
            .Distinct()
            .ToListAsync(ct);

        var advPlayers = await _db.PlayerGameAdvanced
            .Where(a => a.GameId == gameId)
            .Select(a => a.PlayerId)
            .Distinct()
            .ToListAsync(ct);

        var tradCount = tradPlayers.Count;
        var advCount = advPlayers.Count;
        var isComplete = tradCount > 0
            && advCount > 0
            && tradCount == advCount
            && tradPlayers.OrderBy(id => id).SequenceEqual(advPlayers.OrderBy(id => id));

        return new GameCoverageStatus(isComplete, tradCount, advCount);
    }

    private async Task<SeasonCoverageAudit> AuditSeasonCoverageAsync(string seasonLabel, IReadOnlyCollection<string> expectedGameIds,
        CancellationToken ct)
    {
        if (expectedGameIds.Count == 0)
            return new SeasonCoverageAudit(seasonLabel, 0, 0, 0, 0, 0);

        var gameIds = expectedGameIds.Distinct().ToList();
        var gamesInDb = await _db.Games
            .Where(g => gameIds.Contains(g.Id))
            .Select(g => g.Id)
            .ToListAsync(ct);

        var tradCounts = await _db.PlayerGameStats
            .Where(s => gameIds.Contains(s.GameId))
            .GroupBy(s => s.GameId)
            .Select(g => new { GameId = g.Key, Count = g.Select(x => x.PlayerId).Distinct().Count() })
            .ToDictionaryAsync(x => x.GameId, x => x.Count, ct);

        var advCounts = await _db.PlayerGameAdvanced
            .Where(a => gameIds.Contains(a.GameId))
            .GroupBy(a => a.GameId)
            .Select(g => new { GameId = g.Key, Count = g.Select(x => x.PlayerId).Distinct().Count() })
            .ToDictionaryAsync(x => x.GameId, x => x.Count, ct);

        var syncKeys = gameIds.Select(BoxscoreSyncKey).ToList();
        var syncedKeys = await _db.SyncStates
            .Where(s => syncKeys.Contains(s.Key))
            .Select(s => s.Key)
            .ToListAsync(ct);

        int complete = 0;
        int missing = 0;
        int incomplete = 0;
        int orphanedSync = 0;

        foreach (var gameId in gameIds)
        {
            var hasGame = gamesInDb.Contains(gameId);
            tradCounts.TryGetValue(gameId, out var tradCount);
            advCounts.TryGetValue(gameId, out var advCount);

            var isComplete = hasGame && tradCount > 0 && advCount > 0 && tradCount == advCount;
            if (isComplete)
                complete++;
            else if (!hasGame)
                missing++;
            else
                incomplete++;

            if (syncedKeys.Contains(BoxscoreSyncKey(gameId)) && !isComplete)
                orphanedSync++;
        }

        return new SeasonCoverageAudit(seasonLabel, gameIds.Count, complete, incomplete, missing, orphanedSync);
    }

    private async Task MarkGameCompleteAsync(string gameId, CancellationToken ct)
    {
        await UpsertSyncStateAsync(BoxscoreSyncKey(gameId), "done", ct);
        await RemoveSyncStateAsync(BoxscoreRetryKey(gameId), ct);
    }

    private async Task MarkGameIncompleteAsync(string gameId, string reason, CancellationToken ct)
    {
        await RemoveSyncStateAsync(BoxscoreSyncKey(gameId), ct);
        await UpsertSyncStateAsync(BoxscoreRetryKey(gameId), reason, ct);
    }

    private async Task UpsertSyncStateAsync(string key, string value, CancellationToken ct)
    {
        var existing = await _db.SyncStates.FirstOrDefaultAsync(s => s.Key == key, ct);
        if (existing == null)
        {
            _db.SyncStates.Add(new SyncState
            {
                Key = key,
                Value = value,
                UpdatedAt = DateTime.UtcNow,
            });
        }
        else
        {
            existing.Value = value;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task RemoveSyncStateAsync(string key, CancellationToken ct)
    {
        var existing = await _db.SyncStates.FirstOrDefaultAsync(s => s.Key == key, ct);
        if (existing == null)
            return;

        _db.SyncStates.Remove(existing);
        await _db.SaveChangesAsync(ct);
    }

    private static string BoxscoreSyncKey(string gameId) => $"boxscore_{gameId}";
    private static string BoxscoreRetryKey(string gameId) => $"boxscore_retry_{gameId}";

    public sealed record SeasonCoverageAudit(
        string SeasonLabel,
        int ExpectedGames,
        int CompleteGames,
        int IncompleteGames,
        int MissingGames,
        int OrphanedSyncStates)
    {
        public bool IsComplete => ExpectedGames > 0 && ExpectedGames == CompleteGames;
    }

    private sealed record GameSyncAttempt(bool IsSuccessful, string? FailureReason);
    private sealed record GameSyncResult(bool IsComplete, string Status, string? Detail);
    private sealed record GameCoverageStatus(bool IsComplete, int TraditionalPlayers, int AdvancedPlayers);
}
