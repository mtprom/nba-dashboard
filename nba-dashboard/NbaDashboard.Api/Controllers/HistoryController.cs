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
        [FromQuery] int fromSeason = 1996,
        [FromQuery] int toSeason = 2025,
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
                })
                .AsNoTracking()
                .ToListAsync(ct);

            // Step 3: In-memory aggregation

            // -- Metrics --
            var totalGames = games.Count;
            var seasonYearsInData = games.Select(g => seasonMap[g.SeasonId]).Distinct().Count();
            var totalMargin = games.Sum(g => Math.Abs(g.HomeScore - g.VisitorScore));

            var metrics = new HistoryMetricsDto
            {
                TotalGames = totalGames,
                SeasonsCovered = seasonYearsInData,
                AvgMarginOfVictory = totalGames > 0 ? Math.Round((double)totalMargin / totalGames, 1) : 0,
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

            // -- Season stats --
            var seasonStatsList = seasonsInData.Select(sy =>
            {
                var sg = games.Where(g => seasonMap[g.SeasonId] == sy).ToList();
                var gc = sg.Count;

                double? winPct = null, homeWp = null, awayWp = null;
                double? avgTotal = null, leagueHomeWp = null;

                if (teamId.HasValue)
                {
                    var tid = teamId.Value;
                    var wins = sg.Count(g =>
                        (g.HomeTeamId == tid && g.HomeScore > g.VisitorScore) ||
                        (g.VisitorTeamId == tid && g.VisitorScore > g.HomeScore));
                    winPct = gc > 0 ? Math.Round((double)wins / gc, 3) : null;

                    var homeGames = sg.Where(g => g.HomeTeamId == tid).ToList();
                    var awayGames = sg.Where(g => g.VisitorTeamId == tid).ToList();
                    homeWp = homeGames.Count > 0
                        ? Math.Round((double)homeGames.Count(g => g.HomeScore > g.VisitorScore) / homeGames.Count, 3)
                        : null;
                    awayWp = awayGames.Count > 0
                        ? Math.Round((double)awayGames.Count(g => g.VisitorScore > g.HomeScore) / awayGames.Count, 3)
                        : null;
                }
                else
                {
                    avgTotal = gc > 0 ? Math.Round(sg.Average(g => (double)(g.HomeScore + g.VisitorScore)), 1) : null;
                    var homeWins = sg.Count(g => g.HomeScore > g.VisitorScore);
                    leagueHomeWp = gc > 0 ? Math.Round((double)homeWins / gc, 3) : null;
                }

                return new SeasonStatDatumDto
                {
                    SeasonYear = sy,
                    SeasonLabel = $"{sy}-{(sy + 1) % 100:D2}",
                    GameCount = gc,
                    WinPct = winPct,
                    HomeWinPct = homeWp,
                    AwayWinPct = awayWp,
                    AvgTotalPoints = avgTotal,
                    LeagueHomeWinPct = leagueHomeWp,
                    CloseGames = sg.Count(g => Math.Abs(g.HomeScore - g.VisitorScore) <= 5),
                    ModerateGames = sg.Count(g => { var m = Math.Abs(g.HomeScore - g.VisitorScore); return m >= 6 && m <= 19; }),
                    BlowoutGames = sg.Count(g => Math.Abs(g.HomeScore - g.VisitorScore) >= 20),
                };
            }).ToList();

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

            // -- Best/worst games (team mode only) --
            BestWorstGamesDto? bestWorstGames = null;
            if (teamId.HasValue)
            {
                var tid = teamId.Value;
                var teamWins = games
                    .Where(g => (g.HomeTeamId == tid && g.HomeScore > g.VisitorScore) ||
                                (g.VisitorTeamId == tid && g.VisitorScore > g.HomeScore))
                    .ToList();
                var teamLosses = games
                    .Where(g => (g.HomeTeamId == tid && g.HomeScore < g.VisitorScore) ||
                                (g.VisitorTeamId == tid && g.VisitorScore < g.HomeScore))
                    .ToList();

                bestWorstGames = new BestWorstGamesDto
                {
                    LargestWin = teamWins
                        .OrderByDescending(g => Math.Abs(g.HomeScore - g.VisitorScore))
                        .Select(g => ToDto(g))
                        .FirstOrDefault(),
                    LargestLoss = teamLosses
                        .OrderByDescending(g => Math.Abs(g.HomeScore - g.VisitorScore))
                        .Select(g => ToDto(g))
                        .FirstOrDefault(),
                    HighestScoringGame = games
                        .OrderByDescending(g => g.HomeScore + g.VisitorScore)
                        .Select(g => ToDto(g))
                        .FirstOrDefault(),
                    LowestScoringGame = games
                        .Where(g => g.HomeScore > 0 && g.VisitorScore > 0)
                        .OrderBy(g => g.HomeScore + g.VisitorScore)
                        .Select(g => ToDto(g))
                        .FirstOrDefault(),
                };
            }

            // Step 4: Assemble, cache, return
            var result = new HistoryResponseDto
            {
                Metrics = metrics,
                SeasonBarData = seasonBarData,
                HeatmapData = heatmapData,
                ClosestGames = closestGames,
                BlowoutGames = blowoutGames,
                SeasonStats = seasonStatsList,
                BestWorstGames = bestWorstGames,
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
