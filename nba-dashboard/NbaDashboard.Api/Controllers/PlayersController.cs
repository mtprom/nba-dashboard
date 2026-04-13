using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NbaDashboard.Api.DTOs;
using NbaDashboard.Infrastructure.Data;

namespace NbaDashboard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlayersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PlayersController> _logger;

    public PlayersController(AppDbContext db, IMapper mapper, IMemoryCache cache, ILogger<PlayersController> logger)
    {
        _db = db;
        _mapper = mapper;
        _cache = cache;
        _logger = logger;
    }

    [HttpGet("season-averages")]
    public async Task<ActionResult<Dictionary<int, PlayerSeasonAvgDto>>> GetSeasonAverages(
        [FromQuery] string? teamIds, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(teamIds))
            return BadRequest("teamIds parameter is required");

        var ids = teamIds.Split(',')
            .Select(s => int.TryParse(s.Trim(), out var id) ? id : (int?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .OrderBy(id => id)
            .ToList();

        if (ids.Count == 0)
            return BadRequest("No valid team IDs provided");

        var now = DateTime.UtcNow;
        int seasonYear = now.Month >= 10 ? now.Year : now.Year - 1;
        var cacheKey = $"season_avg_{seasonYear}_{string.Join("_", ids)}";

        if (_cache.TryGetValue(cacheKey, out Dictionary<int, PlayerSeasonAvgDto>? cached))
            return Ok(cached);

        try
        {
            var season = await _db.Seasons.FirstOrDefaultAsync(s => s.Year == seasonYear, ct);
            if (season == null)
                return Ok(new Dictionary<int, PlayerSeasonAvgDto>());

            var stats = await _db.PlayerSeasonStats
                .Where(s => s.SeasonId == season.Id && ids.Contains(s.TeamId))
                .Include(s => s.Player)
                .ToListAsync(ct);

            var result = stats.ToDictionary(
                s => s.PlayerId,
                s => _mapper.Map<PlayerSeasonAvgDto>(s));

            _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load season averages for teams {TeamIds}", teamIds);
            return StatusCode(500, new { error = "Failed to load season averages" });
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<PlayerSearchResultDto>>> SearchPlayers(
        [FromQuery] string? query,
        CancellationToken ct)
    {
        var normalized = query?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
            return Ok(new List<PlayerSearchResultDto>());

        var cacheKey = $"player_search_{normalized}";
        if (_cache.TryGetValue(cacheKey, out List<PlayerSearchResultDto>? cached))
            return Ok(cached);

        try
        {
            var todayUtc = DateTime.UtcNow.Date;

            var results = await ValidPlayerGames(todayUtc)
                .Where(s => (s.Player.FirstName + " " + s.Player.LastName).ToLower().Contains(normalized))
                .GroupBy(s => new
                {
                    s.PlayerId,
                    s.Player.FirstName,
                    s.Player.LastName,
                    s.Player.Position,
                    s.Player.JerseyNumber,
                    s.Player.IsActive,
                    CurrentTeamId = s.Player.TeamId,
                    CurrentTeamName = s.Player.Team.FullName,
                    CurrentTeamAbbreviation = s.Player.Team.Abbreviation,
                })
                .Select(g => new PlayerSearchResultDto
                {
                    PlayerId = g.Key.PlayerId,
                    PlayerName = g.Key.FirstName + " " + g.Key.LastName,
                    Position = g.Key.Position,
                    JerseyNumber = g.Key.JerseyNumber,
                    CurrentTeamId = g.Key.CurrentTeamId,
                    CurrentTeamName = g.Key.CurrentTeamName,
                    CurrentTeamAbbreviation = g.Key.CurrentTeamAbbreviation,
                    IsActive = g.Key.IsActive,
                    FirstSeasonYear = g.Min(s => s.Game.Season.Year),
                    LastSeasonYear = g.Max(s => s.Game.Season.Year),
                    GamesPlayed = g.Count(),
                })
                .OrderByDescending(p => p.IsActive)
                .ThenByDescending(p => p.LastSeasonYear)
                .ThenBy(p => p.PlayerName)
                .Take(20)
                .AsNoTracking()
                .ToListAsync(ct);

            _cache.Set(cacheKey, results, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15),
            });

            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search players for query {Query}", query);
            return StatusCode(500, new { error = "Failed to search players" });
        }
    }

    [HttpGet("history")]
    public async Task<ActionResult<PlayerHistoryResponseDto>> GetHistory(
        [FromQuery] int playerId,
        [FromQuery] int fromSeason = 1996,
        [FromQuery] int toSeason = 2025,
        CancellationToken ct = default)
    {
        if (playerId <= 0)
            return BadRequest(new { error = "playerId is required" });

        if (fromSeason > toSeason)
            return BadRequest(new { error = "fromSeason must be <= toSeason" });

        var cacheKey = $"player_history_{playerId}_{fromSeason}_{toSeason}";
        if (_cache.TryGetValue(cacheKey, out PlayerHistoryResponseDto? cached))
            return Ok(cached);

        try
        {
            var todayUtc = DateTime.UtcNow.Date;

            var player = await _db.Players
                .Include(p => p.Team)
                .FirstOrDefaultAsync(p => p.Id == playerId, ct);

            if (player == null)
                return NotFound();

            var availableSeasonYears = await ValidPlayerGames(todayUtc)
                .Where(s => s.PlayerId == playerId)
                .Select(s => s.Game.Season.Year)
                .Distinct()
                .OrderBy(y => y)
                .ToListAsync(ct);

            if (availableSeasonYears.Count == 0)
                return NotFound();

            var rows = await (
                from trad in ValidPlayerGames(todayUtc)
                where trad.PlayerId == playerId
                   && trad.Game.Season.Year >= fromSeason
                   && trad.Game.Season.Year <= toSeason
                join advRow in _db.PlayerGameAdvanced.AsNoTracking()
                    on new { trad.GameId, trad.PlayerId } equals new { advRow.GameId, advRow.PlayerId } into advGroup
                from adv in advGroup.DefaultIfEmpty()
                orderby trad.Game.Date descending, trad.GameId descending
                select new PlayerHistoryRow
                {
                    GameId = trad.GameId,
                    Date = trad.Game.Date,
                    SeasonYear = trad.Game.Season.Year,
                    TeamId = trad.TeamId,
                    OpponentTeamId = trad.TeamId == trad.Game.HomeTeamId ? trad.Game.VisitorTeamId : trad.Game.HomeTeamId,
                    IsHome = trad.TeamId == trad.Game.HomeTeamId,
                    Won = (trad.TeamId == trad.Game.HomeTeamId && trad.Game.HomeScore > trad.Game.VisitorScore)
                        || (trad.TeamId == trad.Game.VisitorTeamId && trad.Game.VisitorScore > trad.Game.HomeScore),
                    HomeTeamId = trad.Game.HomeTeamId,
                    AwayTeamId = trad.Game.VisitorTeamId,
                    HomeScore = trad.Game.HomeScore,
                    AwayScore = trad.Game.VisitorScore,
                    StartPosition = trad.StartPosition,
                    Minutes = trad.Minutes,
                    Points = trad.Points,
                    Rebounds = trad.Rebounds,
                    Assists = trad.Assists,
                    Steals = trad.Steals,
                    Blocks = trad.Blocks,
                    Turnovers = trad.Turnovers,
                    PersonalFouls = trad.PersonalFouls,
                    PlusMinus = trad.PlusMinus,
                    FieldGoalsMade = trad.FieldGoalsMade,
                    FieldGoalsAttempted = trad.FieldGoalsAttempted,
                    FieldGoalPct = trad.FieldGoalPct,
                    ThreePointersMade = trad.ThreePointersMade,
                    ThreePointersAttempted = trad.ThreePointersAttempted,
                    ThreePointPct = trad.ThreePointPct,
                    FreeThrowsMade = trad.FreeThrowsMade,
                    FreeThrowsAttempted = trad.FreeThrowsAttempted,
                    FreeThrowPct = trad.FreeThrowPct,
                    OffensiveRebounds = trad.OffensiveRebounds,
                    DefensiveRebounds = trad.DefensiveRebounds,
                    OffRating = adv != null ? adv.OffRating : null,
                    DefRating = adv != null ? adv.DefRating : null,
                    NetRating = adv != null ? adv.NetRating : null,
                    AstPct = adv != null ? adv.AstPct : null,
                    OrebPct = adv != null ? adv.OrebPct : null,
                    DrebPct = adv != null ? adv.DrebPct : null,
                    RebPct = adv != null ? adv.RebPct : null,
                    EfgPct = adv != null ? adv.EfgPct : null,
                    TsPct = adv != null ? adv.TsPct : null,
                    UsgPct = adv != null ? adv.UsgPct : null,
                    Pace = adv != null ? adv.Pace : null,
                    Pie = adv != null ? adv.Pie : null,
                })
                .ToListAsync(ct);

            var metrics = new PlayerHistoryMetricsDto
            {
                TotalGames = rows.Count,
                SeasonsCovered = rows.Select(r => r.SeasonYear).Distinct().Count(),
                AvgMinutes = RoundAverage(rows, r => r.Minutes),
                AvgPoints = RoundAverage(rows, r => r.Points),
                AvgRebounds = RoundAverage(rows, r => r.Rebounds),
                AvgAssists = RoundAverage(rows, r => r.Assists),
            };

            var seasonStats = rows
                .GroupBy(r => r.SeasonYear)
                .OrderBy(g => g.Key)
                .Select(g => BuildSeasonDatum(g.Key, g.ToList()))
                .ToList();

            var gameLog = rows
                .Select(ToGameDto)
                .ToList();

            var homeAwaySplits = new List<PlayerHistorySplitDto>
            {
                BuildSplit("home", "Home", rows.Where(r => r.IsHome).ToList()),
                BuildSplit("away", "Away", rows.Where(r => !r.IsHome).ToList()),
            };

            var winLossSplits = new List<PlayerHistorySplitDto>
            {
                BuildSplit("wins", "Wins", rows.Where(r => r.Won).ToList()),
                BuildSplit("losses", "Losses", rows.Where(r => !r.Won).ToList()),
            };

            var highlights = new PlayerHistoryHighlightsDto
            {
                HighestPoints = rows
                    .OrderByDescending(r => r.Points)
                    .ThenByDescending(r => r.Date)
                    .Select(ToHighlightDto)
                    .FirstOrDefault(),
                HighestRebounds = rows
                    .OrderByDescending(r => r.Rebounds)
                    .ThenByDescending(r => r.Date)
                    .Select(ToHighlightDto)
                    .FirstOrDefault(),
                HighestAssists = rows
                    .OrderByDescending(r => r.Assists)
                    .ThenByDescending(r => r.Date)
                    .Select(ToHighlightDto)
                    .FirstOrDefault(),
                BestEfficiency = rows
                    .Where(r => r.TsPct.HasValue)
                    .OrderByDescending(r => r.TsPct)
                    .ThenByDescending(r => r.FieldGoalsAttempted + r.FreeThrowsAttempted)
                    .ThenByDescending(r => r.Date)
                    .Select(ToHighlightDto)
                    .FirstOrDefault(),
                WorstShooting = rows
                    .Where(r => r.FieldGoalsAttempted > 0)
                    .OrderBy(r => r.FieldGoalPct)
                    .ThenByDescending(r => r.FieldGoalsAttempted)
                    .ThenByDescending(r => r.Date)
                    .Select(ToHighlightDto)
                    .FirstOrDefault(),
                BestPlusMinus = rows
                    .OrderByDescending(r => r.PlusMinus)
                    .ThenByDescending(r => r.Date)
                    .Select(ToHighlightDto)
                    .FirstOrDefault(),
                WorstPlusMinus = rows
                    .OrderBy(r => r.PlusMinus)
                    .ThenByDescending(r => r.Date)
                    .Select(ToHighlightDto)
                    .FirstOrDefault(),
            };

            var result = new PlayerHistoryResponseDto
            {
                Player = new PlayerHistoryPlayerDto
                {
                    PlayerId = player.Id,
                    PlayerName = $"{player.FirstName} {player.LastName}",
                    Position = player.Position,
                    JerseyNumber = player.JerseyNumber,
                    IsActive = player.IsActive,
                    CurrentTeamId = player.TeamId,
                    CurrentTeamName = player.Team?.FullName ?? string.Empty,
                    CurrentTeamAbbreviation = player.Team?.Abbreviation ?? string.Empty,
                    FirstSeasonYear = availableSeasonYears.First(),
                    LastSeasonYear = availableSeasonYears.Last(),
                },
                AvailableSeasonYears = availableSeasonYears,
                Metrics = metrics,
                GameLog = gameLog,
                SeasonStats = seasonStats,
                HomeAwaySplits = homeAwaySplits,
                WinLossSplits = winLossSplits,
                Highlights = highlights,
            };

            _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load player history for player {PlayerId}, seasons {From}-{To}",
                playerId, fromSeason, toSeason);
            return StatusCode(500, new { error = "Failed to load player history" });
        }
    }

    private IQueryable<NbaDashboard.Core.Entities.PlayerGameStats> ValidPlayerGames(DateTime todayUtc) =>
        _db.PlayerGameStats
            .Where(s => s.Game.Status == "Final"
                     && !s.Game.Postseason
                     && s.Game.HomeScore > 0
                     && s.Game.VisitorScore > 0
                     && s.Game.Date.Date <= todayUtc);

    private static PlayerHistoryGameDto ToGameDto(PlayerHistoryRow row) => new()
    {
        GameId = row.GameId,
        Date = row.Date.ToString("yyyy-MM-dd"),
        SeasonYear = row.SeasonYear,
        TeamId = row.TeamId,
        OpponentTeamId = row.OpponentTeamId,
        IsHome = row.IsHome,
        Won = row.Won,
        HomeTeamId = row.HomeTeamId,
        AwayTeamId = row.AwayTeamId,
        HomeScore = row.HomeScore,
        AwayScore = row.AwayScore,
        StartPosition = row.StartPosition,
        Minutes = Round(row.Minutes, 1),
        Points = row.Points,
        Rebounds = row.Rebounds,
        Assists = row.Assists,
        Steals = row.Steals,
        Blocks = row.Blocks,
        Turnovers = row.Turnovers,
        PersonalFouls = row.PersonalFouls,
        PlusMinus = row.PlusMinus,
        FieldGoalsMade = row.FieldGoalsMade,
        FieldGoalsAttempted = row.FieldGoalsAttempted,
        FieldGoalPct = Round(row.FieldGoalPct, 3),
        ThreePointersMade = row.ThreePointersMade,
        ThreePointersAttempted = row.ThreePointersAttempted,
        ThreePointPct = Round(row.ThreePointPct, 3),
        FreeThrowsMade = row.FreeThrowsMade,
        FreeThrowsAttempted = row.FreeThrowsAttempted,
        FreeThrowPct = Round(row.FreeThrowPct, 3),
        OffensiveRebounds = row.OffensiveRebounds,
        DefensiveRebounds = row.DefensiveRebounds,
        OffRating = RoundNullable(row.OffRating, 1),
        DefRating = RoundNullable(row.DefRating, 1),
        NetRating = RoundNullable(row.NetRating, 1),
        AstPct = RoundNullable(row.AstPct, 3),
        OrebPct = RoundNullable(row.OrebPct, 3),
        DrebPct = RoundNullable(row.DrebPct, 3),
        RebPct = RoundNullable(row.RebPct, 3),
        EfgPct = RoundNullable(row.EfgPct, 3),
        TsPct = RoundNullable(row.TsPct, 3),
        UsgPct = RoundNullable(row.UsgPct, 3),
        Pace = RoundNullable(row.Pace, 1),
        Pie = RoundNullable(row.Pie, 3),
    };

    private static PlayerHistoryHighlightGameDto ToHighlightDto(PlayerHistoryRow row) => new()
    {
        GameId = row.GameId,
        Date = row.Date.ToString("yyyy-MM-dd"),
        SeasonYear = row.SeasonYear,
        TeamId = row.TeamId,
        OpponentTeamId = row.OpponentTeamId,
        IsHome = row.IsHome,
        Won = row.Won,
        HomeTeamId = row.HomeTeamId,
        AwayTeamId = row.AwayTeamId,
        HomeScore = row.HomeScore,
        AwayScore = row.AwayScore,
        Minutes = Round(row.Minutes, 1),
        Points = row.Points,
        Rebounds = row.Rebounds,
        Assists = row.Assists,
        PlusMinus = row.PlusMinus,
        FieldGoalsMade = row.FieldGoalsMade,
        FieldGoalsAttempted = row.FieldGoalsAttempted,
        FieldGoalPct = Round(row.FieldGoalPct, 3),
        TsPct = RoundNullable(row.TsPct, 3),
    };

    private static PlayerHistorySeasonDatumDto BuildSeasonDatum(int seasonYear, IReadOnlyList<PlayerHistoryRow> rows) => new()
    {
        SeasonYear = seasonYear,
        SeasonLabel = FormatSeasonLabel(seasonYear),
        GamesPlayed = rows.Count,
        Minutes = RoundAverage(rows, r => r.Minutes),
        Points = RoundAverage(rows, r => r.Points),
        Rebounds = RoundAverage(rows, r => r.Rebounds),
        Assists = RoundAverage(rows, r => r.Assists),
        Steals = RoundAverage(rows, r => r.Steals),
        Blocks = RoundAverage(rows, r => r.Blocks),
        Turnovers = RoundAverage(rows, r => r.Turnovers),
        PlusMinus = RoundAverage(rows, r => r.PlusMinus),
        FieldGoalPct = Ratio(rows.Sum(r => r.FieldGoalsMade), rows.Sum(r => r.FieldGoalsAttempted)),
        ThreePointPct = Ratio(rows.Sum(r => r.ThreePointersMade), rows.Sum(r => r.ThreePointersAttempted)),
        FreeThrowPct = Ratio(rows.Sum(r => r.FreeThrowsMade), rows.Sum(r => r.FreeThrowsAttempted)),
        EfgPct = RoundAverageNullable(rows, r => r.EfgPct, 3),
        TsPct = RoundAverageNullable(rows, r => r.TsPct, 3),
        NetRating = RoundAverageNullable(rows, r => r.NetRating, 1),
        UsgPct = RoundAverageNullable(rows, r => r.UsgPct, 3),
        AstPct = RoundAverageNullable(rows, r => r.AstPct, 3),
        RebPct = RoundAverageNullable(rows, r => r.RebPct, 3),
        Pie = RoundAverageNullable(rows, r => r.Pie, 3),
    };

    private static PlayerHistorySplitDto BuildSplit(string key, string label, IReadOnlyList<PlayerHistoryRow> rows) => new()
    {
        Key = key,
        Label = label,
        GamesPlayed = rows.Count,
        Minutes = RoundAverage(rows, r => r.Minutes),
        Points = RoundAverage(rows, r => r.Points),
        Rebounds = RoundAverage(rows, r => r.Rebounds),
        Assists = RoundAverage(rows, r => r.Assists),
        Steals = RoundAverage(rows, r => r.Steals),
        Blocks = RoundAverage(rows, r => r.Blocks),
        Turnovers = RoundAverage(rows, r => r.Turnovers),
        PlusMinus = RoundAverage(rows, r => r.PlusMinus),
        FieldGoalPct = Ratio(rows.Sum(r => r.FieldGoalsMade), rows.Sum(r => r.FieldGoalsAttempted)),
        ThreePointPct = Ratio(rows.Sum(r => r.ThreePointersMade), rows.Sum(r => r.ThreePointersAttempted)),
        FreeThrowPct = Ratio(rows.Sum(r => r.FreeThrowsMade), rows.Sum(r => r.FreeThrowsAttempted)),
        EfgPct = RoundAverageNullable(rows, r => r.EfgPct, 3),
        TsPct = RoundAverageNullable(rows, r => r.TsPct, 3),
        NetRating = RoundAverageNullable(rows, r => r.NetRating, 1),
        UsgPct = RoundAverageNullable(rows, r => r.UsgPct, 3),
        AstPct = RoundAverageNullable(rows, r => r.AstPct, 3),
        RebPct = RoundAverageNullable(rows, r => r.RebPct, 3),
        Pie = RoundAverageNullable(rows, r => r.Pie, 3),
    };

    private static string FormatSeasonLabel(int year) => $"{year}-{(year + 1) % 100:D2}";

    private static double RoundAverage<T>(IEnumerable<T> rows, Func<T, decimal> selector, int digits = 1)
    {
        var list = rows.ToList();
        if (list.Count == 0) return 0;
        return Math.Round((double)list.Average(selector), digits);
    }

    private static double RoundAverage<T>(IEnumerable<T> rows, Func<T, int> selector, int digits = 1)
    {
        var list = rows.ToList();
        if (list.Count == 0) return 0;
        return Math.Round(list.Average(selector), digits);
    }

    private static double? RoundAverageNullable<T>(IEnumerable<T> rows, Func<T, decimal?> selector, int digits)
    {
        var values = rows.Select(selector).Where(v => v.HasValue).Select(v => v!.Value).ToList();
        if (values.Count == 0) return null;
        return Math.Round((double)values.Average(), digits);
    }

    private static double Ratio(int made, int attempted)
    {
        if (attempted <= 0) return 0;
        return Math.Round((double)made / attempted, 3);
    }

    private static double Round(decimal value, int digits) => Math.Round((double)value, digits);

    private static double? RoundNullable(decimal? value, int digits) =>
        value.HasValue ? Math.Round((double)value.Value, digits) : null;

    private sealed class PlayerHistoryRow
    {
        public string GameId { get; init; } = string.Empty;
        public DateTime Date { get; init; }
        public int SeasonYear { get; init; }
        public int TeamId { get; init; }
        public int OpponentTeamId { get; init; }
        public bool IsHome { get; init; }
        public bool Won { get; init; }
        public int HomeTeamId { get; init; }
        public int AwayTeamId { get; init; }
        public int HomeScore { get; init; }
        public int AwayScore { get; init; }
        public string StartPosition { get; init; } = string.Empty;
        public decimal Minutes { get; init; }
        public int Points { get; init; }
        public int Rebounds { get; init; }
        public int Assists { get; init; }
        public int Steals { get; init; }
        public int Blocks { get; init; }
        public int Turnovers { get; init; }
        public int PersonalFouls { get; init; }
        public int PlusMinus { get; init; }
        public int FieldGoalsMade { get; init; }
        public int FieldGoalsAttempted { get; init; }
        public decimal FieldGoalPct { get; init; }
        public int ThreePointersMade { get; init; }
        public int ThreePointersAttempted { get; init; }
        public decimal ThreePointPct { get; init; }
        public int FreeThrowsMade { get; init; }
        public int FreeThrowsAttempted { get; init; }
        public decimal FreeThrowPct { get; init; }
        public int OffensiveRebounds { get; init; }
        public int DefensiveRebounds { get; init; }
        public decimal? OffRating { get; init; }
        public decimal? DefRating { get; init; }
        public decimal? NetRating { get; init; }
        public decimal? AstPct { get; init; }
        public decimal? OrebPct { get; init; }
        public decimal? DrebPct { get; init; }
        public decimal? RebPct { get; init; }
        public decimal? EfgPct { get; init; }
        public decimal? TsPct { get; init; }
        public decimal? UsgPct { get; init; }
        public decimal? Pace { get; init; }
        public decimal? Pie { get; init; }
    }
}
