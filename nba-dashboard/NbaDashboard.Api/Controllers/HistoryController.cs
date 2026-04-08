using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NbaDashboard.Api.DTOs;
using NbaDashboard.Infrastructure.Data;

namespace NbaDashboard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HistoryController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<HistoryController> _logger;

    private static readonly Dictionary<int, string> SeasonAnnotations = new()
    {
        [1998] = "lockout",
        [2011] = "lockout",
        [2019] = "bubble",
    };

    private static readonly HashSet<int> HeatmapMonths = [10, 11, 12, 1, 2, 3, 4];
    private static readonly int[] HeatmapMonthOrder = [10, 11, 12, 1, 2, 3, 4];

    public HistoryController(AppDbContext db, IMemoryCache cache, ILogger<HistoryController> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<HistoryResponseDto>> GetHistory(
        [FromQuery] int? teamId,
        [FromQuery] int fromSeason = 2000,
        [FromQuery] int toSeason = 2024,
        CancellationToken ct = default)
    {
        if (fromSeason > toSeason)
            return BadRequest(new { error = "fromSeason must be <= toSeason" });

        var cacheKey = $"history_{teamId ?? 0}_{fromSeason}_{toSeason}";
        if (_cache.TryGetValue(cacheKey, out HistoryResponseDto? cached))
            return Ok(cached);

        try
        {
            // Step 1: Resolve season IDs → year mapping
            var seasonMap = await _db.Seasons
                .Where(s => s.Year >= fromSeason && s.Year <= toSeason)
                .ToDictionaryAsync(s => s.Id, s => s.Year, ct);

            var seasonIds = seasonMap.Keys.ToList();

            // Step 2: Single projected query
            var baseQuery = _db.Games
                .Where(g => g.Status == "Final"
                         && !g.Postseason
                         && seasonIds.Contains(g.SeasonId));

            if (teamId.HasValue)
            {
                var tid = teamId.Value;
                baseQuery = baseQuery.Where(g => g.HomeTeamId == tid || g.VisitorTeamId == tid);
            }

            var games = await baseQuery
                .Select(g => new
                {
                    g.Id,
                    g.Date,
                    g.SeasonId,
                    g.HomeTeamId,
                    g.VisitorTeamId,
                    g.HomeScore,
                    g.VisitorScore,
                    g.Period,
                })
                .AsNoTracking()
                .ToListAsync(ct);

            // Step 3: In-memory aggregation

            // -- Metrics --
            var totalGames = games.Count;
            var seasonYearsInData = games.Select(g => seasonMap[g.SeasonId]).Distinct().Count();
            var totalMargin = games.Sum(g => Math.Abs(g.HomeScore - g.VisitorScore));
            var otCount = games.Count(g => g.Period > 4);

            var metrics = new HistoryMetricsDto
            {
                TotalGames = totalGames,
                SeasonsCovered = seasonYearsInData,
                AvgMarginOfVictory = totalGames > 0 ? Math.Round((double)totalMargin / totalGames, 1) : 0,
                OvertimeRate = totalGames > 0 ? Math.Round((double)otCount / totalGames * 100, 1) : 0,
            };

            // -- Season bar data --
            var countsBySeason = games
                .GroupBy(g => seasonMap[g.SeasonId])
                .ToDictionary(grp => grp.Key, grp => grp.Count());

            var seasonBarData = Enumerable.Range(fromSeason, toSeason - fromSeason + 1)
                .Select(y => new SeasonBarDatumDto
                {
                    SeasonYear = y,
                    SeasonLabel = $"{y}-{(y + 1) % 100:D2}",
                    GameCount = countsBySeason.GetValueOrDefault(y),
                    Annotation = SeasonAnnotations.GetValueOrDefault(y),
                })
                .ToList();

            // -- Heatmap data --
            var heatmapGames = games.Where(g => HeatmapMonths.Contains(g.Date.Month));

            var heatmapGroups = heatmapGames
                .GroupBy(g => (SeasonYear: seasonMap[g.SeasonId], Month: g.Date.Month))
                .ToDictionary(grp => grp.Key, grp =>
                {
                    int wins, losses;
                    if (teamId.HasValue)
                    {
                        var tid = teamId.Value;
                        wins = grp.Count(g =>
                            (g.HomeTeamId == tid && g.HomeScore > g.VisitorScore) ||
                            (g.VisitorTeamId == tid && g.VisitorScore > g.HomeScore));
                        losses = grp.Count() - wins;
                    }
                    else
                    {
                        wins = grp.Count(g => g.HomeScore > g.VisitorScore);
                        losses = grp.Count() - wins;
                    }
                    return (Wins: wins, Losses: losses);
                });

            var seasonsInData = games
                .Select(g => seasonMap[g.SeasonId])
                .Distinct()
                .OrderBy(y => y)
                .ToList();

            var heatmapData = new List<HeatmapCellDto>();
            foreach (var sy in seasonsInData)
            {
                foreach (var mo in HeatmapMonthOrder)
                {
                    var key = (SeasonYear: sy, Month: mo);
                    if (heatmapGroups.TryGetValue(key, out var cell))
                    {
                        var total = cell.Wins + cell.Losses;
                        heatmapData.Add(new HeatmapCellDto
                        {
                            SeasonYear = sy,
                            Month = mo,
                            Wins = cell.Wins,
                            Losses = cell.Losses,
                            WinPct = total > 0 ? Math.Round((double)cell.Wins / total, 3) : null,
                        });
                    }
                    else
                    {
                        heatmapData.Add(new HeatmapCellDto
                        {
                            SeasonYear = sy,
                            Month = mo,
                            Wins = 0,
                            Losses = 0,
                            WinPct = null,
                        });
                    }
                }
            }

            // -- Game log subsets --
            HistoryGameDto ToDto(dynamic g) => new()
            {
                Id = g.Id,
                Date = ((DateTime)g.Date).ToString("yyyy-MM-dd"),
                SeasonYear = seasonMap[(int)g.SeasonId],
                Month = ((DateTime)g.Date).Month,
                HomeTeamId = g.HomeTeamId,
                AwayTeamId = g.VisitorTeamId,
                HomeScore = g.HomeScore,
                AwayScore = g.VisitorScore,
                IsOT = g.Period > 4,
            };

            var closestGames = games
                .OrderBy(g => Math.Abs(g.HomeScore - g.VisitorScore))
                .ThenByDescending(g => g.Date)
                .Take(20)
                .Select(g => ToDto(g))
                .ToList();

            var blowoutGames = games
                .Where(g => Math.Abs(g.HomeScore - g.VisitorScore) >= 20)
                .OrderByDescending(g => Math.Abs(g.HomeScore - g.VisitorScore))
                .ThenByDescending(g => g.Date)
                .Take(20)
                .Select(g => ToDto(g))
                .ToList();

            var otGames = games
                .Where(g => g.Period > 4)
                .OrderByDescending(g => g.Date)
                .Take(20)
                .Select(g => ToDto(g))
                .ToList();

            // Step 4: Assemble, cache, return
            var result = new HistoryResponseDto
            {
                Metrics = metrics,
                SeasonBarData = seasonBarData,
                HeatmapData = heatmapData,
                ClosestGames = closestGames,
                BlowoutGames = blowoutGames,
                OtGames = otGames,
            };

            _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load history for team {TeamId}, seasons {From}-{To}",
                teamId, fromSeason, toSeason);
            return StatusCode(500, new { error = "Failed to load game history" });
        }
    }
}
