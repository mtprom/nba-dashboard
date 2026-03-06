using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NbaDashboard.Core.Entities;
using NbaDashboard.Infrastructure.Data;
using NbaDashboard.Infrastructure.NbaStats;
using NbaDashboard.Infrastructure.NbaStats.Models;

namespace NbaDashboard.Worker.Jobs;

public class SyncBoxScoresJob
{
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
        var date = targetDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var dateStr = date.ToString("yyyy-MM-dd");
        _logger.LogInformation("SyncBoxScoresJob starting for {Date}", dateStr);

        // 1. Find game IDs for the target date
        var finderResp = await _nba.GetAsync<LeagueGameFinderResponse>(
            "leaguegamefinder",
            new Dictionary<string, string>
            {
                ["LeagueID"]   = "00",
                ["Season"]     = SeasonStringForDate(date),
                ["SeasonType"] = "Regular Season",
                ["DateFrom"]   = dateStr,
                ["DateTo"]     = dateStr,
            },
            ct);

        var resultSet = finderResp?.ResultSets?.FirstOrDefault();
        if (resultSet == null || resultSet.RowSet.Count == 0)
        {
            _logger.LogInformation("No games found for {Date}", dateStr);
            return;
        }

        var headers = resultSet.Headers;
        int gameIdIdx  = headers.IndexOf("GAME_ID");
        int seasonIdIdx = headers.IndexOf("SEASON_ID");

        // Each game appears twice (one row per team) — deduplicate on GAME_ID
        var gameIds = resultSet.RowSet
            .Select(row => row[gameIdIdx].GetString()!)
            .Distinct()
            .ToList();

        _logger.LogInformation("Found {Count} games for {Date}", gameIds.Count, dateStr);

        // 2. Ensure season exists (derive year from SEASON_ID e.g. "22024" → 2024)
        var seasonIdStr = resultSet.RowSet[0][seasonIdIdx].GetString()!;
        var seasonYear = int.Parse(seasonIdStr[1..]); // "22024" → "2024"
        var season = await UpsertSeasonAsync(seasonYear, ct);

        // 3. For each game: fetch traditional + advanced, save everything
        foreach (var gameId in gameIds)
        {
            var syncKey = $"boxscore_{gameId}";
            if (await _db.SyncStates.AnyAsync(s => s.Key == syncKey, ct))
            {
                _logger.LogDebug("Skipping {GameId} — already synced", gameId);
                continue;
            }

            try
            {
                await SyncGameAsync(gameId, season, date, ct);

                _db.SyncStates.Add(new SyncState
                {
                    Key       = syncKey,
                    Value     = "done",
                    UpdatedAt = DateTime.UtcNow,
                });
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync game {GameId}", gameId);
            }
        }

        _logger.LogInformation("SyncBoxScoresJob complete for {Date}", dateStr);
    }

    private async Task SyncGameAsync(string gameId, Season season, DateOnly date, CancellationToken ct)
    {
        _logger.LogInformation("Syncing game {GameId}", gameId);

        // --- Traditional box score ---
        var tradResp = await _nba.GetAsync<BoxScoreTraditionalV3Response>(
            "boxscoretraditionalv3",
            new Dictionary<string, string> { ["GameID"] = gameId },
            ct);

        var trad = tradResp?.BoxScoreTraditional;
        if (trad == null)
        {
            _logger.LogWarning("No traditional data for {GameId}", gameId);
            return;
        }

        // Upsert teams
        await UpsertTeamAsync(trad.HomeTeam.TeamId, trad.HomeTeam.TeamCity,
            trad.HomeTeam.TeamName, trad.HomeTeam.TeamTricode, ct);
        await UpsertTeamAsync(trad.AwayTeam.TeamId, trad.AwayTeam.TeamCity,
            trad.AwayTeam.TeamName, trad.AwayTeam.TeamTricode, ct);

        // Upsert players from both teams
        foreach (var p in trad.HomeTeam.Players)
            await UpsertPlayerAsync(p.PersonId, p.FirstName, p.FamilyName,
                p.Position, p.JerseyNum, trad.HomeTeam.TeamId, ct);
        foreach (var p in trad.AwayTeam.Players)
            await UpsertPlayerAsync(p.PersonId, p.FirstName, p.FamilyName,
                p.Position, p.JerseyNum, trad.AwayTeam.TeamId, ct);

        // Compute scores by summing player points
        var homeScore = trad.HomeTeam.Players.Sum(p => p.Statistics.Points);
        var awayScore = trad.AwayTeam.Players.Sum(p => p.Statistics.Points);

        // Upsert game
        var game = await _db.Games.FindAsync([gameId], ct);
        if (game == null)
        {
            game = new Game
            {
                Id            = gameId,
                SeasonId      = season.Id,
                Date          = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                Status        = "Final",
                HomeTeamId    = trad.HomeTeamId,
                VisitorTeamId = trad.AwayTeamId,
                HomeScore     = homeScore,
                VisitorScore  = awayScore,
            };
            _db.Games.Add(game);
        }
        else
        {
            game.HomeScore    = homeScore;
            game.VisitorScore = awayScore;
            game.Status       = "Final";
        }
        await _db.SaveChangesAsync(ct);

        // Upsert PlayerGameStats for both teams
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
                    GameId               = gameId,
                    PlayerId             = p.PersonId,
                    TeamId               = teamId,
                    StartPosition        = p.Position,
                    Minutes              = s.MinutesDecimal(),
                    Points               = s.Points,
                    Rebounds             = s.ReboundsTotal,
                    Assists              = s.Assists,
                    Steals               = s.Steals,
                    Blocks               = s.Blocks,
                    Turnovers            = s.Turnovers,
                    PersonalFouls        = s.FoulsPersonal,
                    PlusMinus            = (int)s.PlusMinusPoints,
                    FieldGoalsMade       = s.FieldGoalsMade,
                    FieldGoalsAttempted  = s.FieldGoalsAttempted,
                    FieldGoalPct         = (decimal)s.FieldGoalsPercentage,
                    ThreePointersMade    = s.ThreePointersMade,
                    ThreePointersAttempted = s.ThreePointersAttempted,
                    ThreePointPct        = (decimal)s.ThreePointersPercentage,
                    FreeThrowsMade       = s.FreeThrowsMade,
                    FreeThrowsAttempted  = s.FreeThrowsAttempted,
                    FreeThrowPct         = (decimal)s.FreeThrowsPercentage,
                    OffensiveRebounds    = s.ReboundsOffensive,
                    DefensiveRebounds    = s.ReboundsDefensive,
                });
            }
        }
        await _db.SaveChangesAsync(ct);

        // --- Advanced box score ---
        var advResp = await _nba.GetAsync<BoxScoreAdvancedV3Response>(
            "boxscoreadvancedv3",
            new Dictionary<string, string> { ["GameID"] = gameId },
            ct);

        var adv = advResp?.BoxScoreAdvanced;
        if (adv == null)
        {
            _logger.LogWarning("No advanced data for {GameId}", gameId);
            return;
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
                    GameId    = gameId,
                    PlayerId  = p.PersonId,
                    TeamId    = teamId,
                    Minutes   = s.MinutesDecimal(),
                    OffRating = (decimal)s.OffensiveRating,
                    DefRating = (decimal)s.DefensiveRating,
                    NetRating = (decimal)s.NetRating,
                    AstPct    = (decimal)s.AssistPercentage,
                    OrebPct   = (decimal)s.OffensiveReboundPercentage,
                    DrebPct   = (decimal)s.DefensiveReboundPercentage,
                    RebPct    = (decimal)s.ReboundPercentage,
                    EfgPct    = (decimal)s.EffectiveFieldGoalPercentage,
                    TsPct     = (decimal)s.TrueShootingPercentage,
                    UsgPct    = (decimal)s.UsagePercentage,
                    Pace      = (decimal)s.Pace,
                    Pie       = (decimal)s.PIE,
                });
            }
        }
        await _db.SaveChangesAsync(ct);
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
        var team = await _db.Teams.FindAsync([id], ct);
        if (team == null)
        {
            _db.Teams.Add(new Team
            {
                Id           = id,
                City         = city,
                Name         = name,
                FullName     = $"{city} {name}",
                Abbreviation = tricode,
                UpdatedAt    = DateTime.UtcNow,
            });
        }
        else
        {
            team.Abbreviation = tricode;
            team.UpdatedAt    = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
    }

    private async Task UpsertPlayerAsync(int id, string firstName, string familyName,
        string position, string jerseyNum, int teamId, CancellationToken ct)
    {
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
            player.Position     = position;
            player.JerseyNumber = jerseyNum;
            player.UpdatedAt    = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task RunForSeasonAsync(int seasonStartYear, CancellationToken ct = default)
    {
        var seasonStr = $"{seasonStartYear}-{(seasonStartYear + 1) % 100:D2}";
        _logger.LogInformation("Fetching all games for season {Season}", seasonStr);

        var finderResp = await _nba.GetAsync<LeagueGameFinderResponse>(
            "leaguegamefinder",
            new Dictionary<string, string>
            {
                ["LeagueID"]   = "00",
                ["Season"]     = seasonStr,
                ["SeasonType"] = "Regular Season",
            },
            ct);

        var resultSet = finderResp?.ResultSets?.FirstOrDefault();
        if (resultSet == null || resultSet.RowSet.Count == 0)
        {
            _logger.LogInformation("No games found for season {Season}", seasonStr);
            return;
        }

        var headers = resultSet.Headers;
        int gameIdIdx   = headers.IndexOf("GAME_ID");
        int gameDateIdx = headers.IndexOf("GAME_DATE");
        int seasonIdIdx = headers.IndexOf("SEASON_ID");

        var games = resultSet.RowSet
            .GroupBy(row => row[gameIdIdx].GetString()!)
            .Select(g => (
                GameId: g.Key,
                Date: DateOnly.Parse(g.First()[gameDateIdx].GetString()!)
            ))
            .OrderByDescending(g => g.Date)
            .ToList();

        _logger.LogInformation("Found {Count} unique games for season {Season}", games.Count, seasonStr);

        var seasonIdStr = resultSet.RowSet[0][seasonIdIdx].GetString()!;
        var seasonYear = int.Parse(seasonIdStr[1..]);
        var season = await UpsertSeasonAsync(seasonYear, ct);

        int synced = 0;
        foreach (var (gameId, date) in games)
        {
            ct.ThrowIfCancellationRequested();

            var syncKey = $"boxscore_{gameId}";
            if (await _db.SyncStates.AnyAsync(s => s.Key == syncKey, ct))
            {
                synced++;
                continue;
            }

            try
            {
                await SyncGameAsync(gameId, season, date, ct);

                _db.SyncStates.Add(new SyncState
                {
                    Key       = syncKey,
                    Value     = "done",
                    UpdatedAt = DateTime.UtcNow,
                });
                await _db.SaveChangesAsync(ct);
                synced++;
                _logger.LogInformation("Synced {GameId} ({Synced}/{Total})", gameId, synced, games.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync game {GameId}", gameId);
            }
        }

        _logger.LogInformation("Season {Season} complete: {Synced}/{Total} games synced",
            seasonStr, synced, games.Count);
    }

    // Converts a date to NBA season string e.g. date in 2024-25 season → "2024-25"
    private static string SeasonStringForDate(DateOnly date)
    {
        // NBA season starts in October; if month < 7 we're still in prior year's season
        int startYear = date.Month >= 10 ? date.Year : date.Year - 1;
        return $"{startYear}-{(startYear + 1) % 100:D2}";
    }
}
