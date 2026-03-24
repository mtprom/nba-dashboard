using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NbaDashboard.Api.DTOs;
using NbaDashboard.Infrastructure.Data;

namespace NbaDashboard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HotController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;
    private readonly ILogger<HotController> _logger;

    public HotController(AppDbContext db, IMapper mapper, IMemoryCache cache, ILogger<HotController> logger)
    {
        _db = db;
        _mapper = mapper;
        _cache = cache;
        _logger = logger;
    }

    [HttpGet("players")]
    public async Task<ActionResult<List<HotPlayerDto>>> GetHotPlayers(
        [FromQuery] string window = "5", CancellationToken ct = default)
    {
        var cacheKey = $"hot_players_{window}";
        if (_cache.TryGetValue(cacheKey, out List<HotPlayerDto>? cached))
            return Ok(cached);

        try
        {
            var now = DateTime.UtcNow;
            int currentSeasonYear = now.Month >= 10 ? now.Year : now.Year - 1;
            var currentSeason = await _db.Seasons.FirstOrDefaultAsync(s => s.Year == currentSeasonYear, ct);
            if (currentSeason == null)
                return Ok(new List<HotPlayerDto>());

            List<HotPlayerDto> result;

            if (window == "season")
            {
                result = await ComputeSeasonVsLastSeason(currentSeason.Id, currentSeasonYear, ct);
            }
            else if (int.TryParse(window, out var n) && (n == 5 || n == 10))
            {
                result = await ComputeLastNGames(currentSeason.Id, n, ct);
            }
            else
            {
                return BadRequest("window must be 5, 10, or season");
            }

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compute hot players for window={Window}", window);
            return StatusCode(500, new { error = "Failed to compute hot players" });
        }
    }

    [HttpGet("teams")]
    public async Task<ActionResult<List<HotTeamDto>>> GetHotTeams(
        [FromQuery] string window = "5", CancellationToken ct = default)
    {
        var cacheKey = $"hot_teams_{window}";
        if (_cache.TryGetValue(cacheKey, out List<HotTeamDto>? cached))
            return Ok(cached);

        try
        {
            var now = DateTime.UtcNow;
            int currentSeasonYear = now.Month >= 10 ? now.Year : now.Year - 1;
            var currentSeason = await _db.Seasons.FirstOrDefaultAsync(s => s.Year == currentSeasonYear, ct);
            if (currentSeason == null)
                return Ok(new List<HotTeamDto>());

            List<HotTeamDto> result;

            if (window == "season")
            {
                result = await ComputeTeamSeasonVsLast(currentSeason.Id, currentSeasonYear, ct);
            }
            else if (int.TryParse(window, out var n) && (n == 5 || n == 10))
            {
                result = await ComputeTeamLastNGames(currentSeason.Id, n, ct);
            }
            else
            {
                return BadRequest("window must be 5, 10, or season");
            }

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compute hot teams for window={Window}", window);
            return StatusCode(500, new { error = "Failed to compute hot teams" });
        }
    }

    // ─── Hot Players: Last N Games vs Season Average ───

    private async Task<List<HotPlayerDto>> ComputeLastNGames(int seasonId, int n, CancellationToken ct)
    {
        // Get all season averages for current season (baseline)
        var seasonStats = await _db.PlayerSeasonStats
            .Where(s => s.SeasonId == seasonId && s.GamesPlayed >= n)
            .Include(s => s.Player).ThenInclude(p => p.Team)
            .ToListAsync(ct);

        if (seasonStats.Count == 0)
            return [];

        var playerIds = seasonStats.Select(s => s.PlayerId).Distinct().ToList();

        // Get the last N game IDs for each player in one query
        // First get all games this season ordered by date
        var seasonGameIds = await _db.Games
            .Where(g => g.SeasonId == seasonId && g.Status == "Final")
            .OrderByDescending(g => g.Date)
            .Select(g => g.Id)
            .ToListAsync(ct);

        // Get traditional stats for these players in season games
        var allTradStats = await _db.PlayerGameStats
            .Where(s => playerIds.Contains(s.PlayerId) && seasonGameIds.Contains(s.GameId))
            .Include(s => s.Game)
            .OrderByDescending(s => s.Game.Date)
            .ToListAsync(ct);

        // Get advanced stats
        var allAdvStats = await _db.PlayerGameAdvanced
            .Where(s => playerIds.Contains(s.PlayerId) && seasonGameIds.Contains(s.GameId))
            .ToDictionaryAsync(s => (s.PlayerId, s.GameId), ct);

        var result = new List<HotPlayerDto>();

        foreach (var baseline in seasonStats)
        {
            ct.ThrowIfCancellationRequested();

            var recentGames = allTradStats
                .Where(s => s.PlayerId == baseline.PlayerId)
                .Take(n)
                .ToList();

            if (recentGames.Count < n)
                continue;

            var count = (decimal)recentGames.Count;
            var ptsAvg = recentGames.Sum(g => (decimal)g.Points) / count;
            var rebAvg = recentGames.Sum(g => (decimal)g.Rebounds) / count;
            var astAvg = recentGames.Sum(g => (decimal)g.Assists) / count;
            var fgPct = recentGames.Average(g => g.FieldGoalPct);

            // Advanced stats averages
            decimal tsPct = 0m, netRating = 0m;
            int advCount = 0;
            foreach (var g in recentGames)
            {
                if (allAdvStats.TryGetValue((g.PlayerId, g.GameId), out var adv))
                {
                    tsPct += adv.TsPct;
                    netRating += adv.NetRating;
                    advCount++;
                }
            }
            if (advCount > 0)
            {
                tsPct /= advCount;
                netRating /= advCount;
            }

            var ptsDelta = ptsAvg - baseline.PtsAvg;
            var rebDelta = rebAvg - baseline.RebAvg;
            var astDelta = astAvg - baseline.AstAvg;
            var fgPctDelta = fgPct - baseline.FgPct;
            var tsPctDelta = tsPct - baseline.TsPct;
            var netRatingDelta = netRating - baseline.NetRating;

            // Composite heat score (weighted, normalized by typical ranges)
            var heatScore =
                (ptsDelta / 5m) * 0.35m +        // ~5 pts swing is significant
                (tsPctDelta / 0.05m) * 0.25m +   // ~5% TS swing is significant
                (astDelta / 3m) * 0.15m +         // ~3 ast swing is significant
                (netRatingDelta / 10m) * 0.15m +  // ~10 NetRtg swing is significant
                (rebDelta / 3m) * 0.10m;          // ~3 reb swing is significant

            result.Add(new HotPlayerDto
            {
                PlayerId = baseline.PlayerId,
                PlayerName = $"{baseline.Player.FirstName} {baseline.Player.LastName}",
                Position = baseline.Player.Position,
                JerseyNumber = baseline.Player.JerseyNumber,
                Team = _mapper.Map<TeamDto>(baseline.Player.Team),
                HeatScore = Math.Round(heatScore, 3),
                GamesPlayed = recentGames.Count,
                PtsAvg = Math.Round(ptsAvg, 1),
                RebAvg = Math.Round(rebAvg, 1),
                AstAvg = Math.Round(astAvg, 1),
                FgPct = Math.Round(fgPct, 3),
                TsPct = Math.Round(tsPct, 3),
                NetRating = Math.Round(netRating, 1),
                BaselinePtsAvg = Math.Round(baseline.PtsAvg, 1),
                BaselineRebAvg = Math.Round(baseline.RebAvg, 1),
                BaselineAstAvg = Math.Round(baseline.AstAvg, 1),
                BaselineFgPct = Math.Round(baseline.FgPct, 3),
                BaselineTsPct = Math.Round(baseline.TsPct, 3),
                BaselineNetRating = Math.Round(baseline.NetRating, 1),
                PtsDelta = Math.Round(ptsDelta, 1),
                RebDelta = Math.Round(rebDelta, 1),
                AstDelta = Math.Round(astDelta, 1),
                FgPctDelta = Math.Round(fgPctDelta, 3),
                TsPctDelta = Math.Round(tsPctDelta, 3),
                NetRatingDelta = Math.Round(netRatingDelta, 1),
            });
        }

        return result.OrderByDescending(p => p.HeatScore).Take(50).ToList();
    }

    // ─── Hot Players: This Season vs Last Season ───

    private async Task<List<HotPlayerDto>> ComputeSeasonVsLastSeason(
        int currentSeasonId, int currentSeasonYear, CancellationToken ct)
    {
        var prevSeason = await _db.Seasons.FirstOrDefaultAsync(s => s.Year == currentSeasonYear - 1, ct);
        if (prevSeason == null)
            return [];

        var currentStats = await _db.PlayerSeasonStats
            .Where(s => s.SeasonId == currentSeasonId && s.GamesPlayed >= 10)
            .Include(s => s.Player).ThenInclude(p => p.Team)
            .ToDictionaryAsync(s => s.PlayerId, ct);

        var prevStats = await _db.PlayerSeasonStats
            .Where(s => s.SeasonId == prevSeason.Id && s.GamesPlayed >= 10)
            .ToDictionaryAsync(s => s.PlayerId, ct);

        var result = new List<HotPlayerDto>();

        foreach (var (playerId, current) in currentStats)
        {
            if (!prevStats.TryGetValue(playerId, out var prev))
                continue;

            var ptsDelta = current.PtsAvg - prev.PtsAvg;
            var rebDelta = current.RebAvg - prev.RebAvg;
            var astDelta = current.AstAvg - prev.AstAvg;
            var fgPctDelta = current.FgPct - prev.FgPct;
            var tsPctDelta = current.TsPct - prev.TsPct;
            var netRatingDelta = current.NetRating - prev.NetRating;

            var heatScore =
                (ptsDelta / 5m) * 0.35m +
                (tsPctDelta / 0.05m) * 0.25m +
                (astDelta / 3m) * 0.15m +
                (netRatingDelta / 10m) * 0.15m +
                (rebDelta / 3m) * 0.10m;

            result.Add(new HotPlayerDto
            {
                PlayerId = playerId,
                PlayerName = $"{current.Player.FirstName} {current.Player.LastName}",
                Position = current.Player.Position,
                JerseyNumber = current.Player.JerseyNumber,
                Team = _mapper.Map<TeamDto>(current.Player.Team),
                HeatScore = Math.Round(heatScore, 3),
                GamesPlayed = current.GamesPlayed,
                PtsAvg = Math.Round(current.PtsAvg, 1),
                RebAvg = Math.Round(current.RebAvg, 1),
                AstAvg = Math.Round(current.AstAvg, 1),
                FgPct = Math.Round(current.FgPct, 3),
                TsPct = Math.Round(current.TsPct, 3),
                NetRating = Math.Round(current.NetRating, 1),
                BaselinePtsAvg = Math.Round(prev.PtsAvg, 1),
                BaselineRebAvg = Math.Round(prev.RebAvg, 1),
                BaselineAstAvg = Math.Round(prev.AstAvg, 1),
                BaselineFgPct = Math.Round(prev.FgPct, 3),
                BaselineTsPct = Math.Round(prev.TsPct, 3),
                BaselineNetRating = Math.Round(prev.NetRating, 1),
                PtsDelta = Math.Round(ptsDelta, 1),
                RebDelta = Math.Round(rebDelta, 1),
                AstDelta = Math.Round(astDelta, 1),
                FgPctDelta = Math.Round(fgPctDelta, 3),
                TsPctDelta = Math.Round(tsPctDelta, 3),
                NetRatingDelta = Math.Round(netRatingDelta, 1),
            });
        }

        return result.OrderByDescending(p => p.HeatScore).Take(50).ToList();
    }

    // ─── Hot Teams: Last N Games vs Season ───

    private async Task<List<HotTeamDto>> ComputeTeamLastNGames(int seasonId, int n, CancellationToken ct)
    {
        var teams = await _db.Teams.Where(t => !string.IsNullOrEmpty(t.Conference)).ToListAsync(ct);
        if (teams.Count == 0)
            return [];

        // Get latest standings for baseline
        var latestDate = await _db.StandingsSnapshots
            .Where(s => s.SeasonId == seasonId)
            .MaxAsync(s => (DateTime?)s.SnapshotDate, ct);

        var standings = latestDate != null
            ? await _db.StandingsSnapshots
                .Where(s => s.SeasonId == seasonId && s.SnapshotDate == latestDate.Value)
                .ToDictionaryAsync(s => s.TeamId, ct)
            : new Dictionary<int, NbaDashboard.Core.Entities.StandingsSnapshot>();

        // Get all final games this season
        var allGames = await _db.Games
            .Where(g => g.SeasonId == seasonId && g.Status == "Final")
            .OrderByDescending(g => g.Date)
            .ToListAsync(ct);

        var result = new List<HotTeamDto>();

        foreach (var team in teams)
        {
            var teamGames = allGames
                .Where(g => g.HomeTeamId == team.Id || g.VisitorTeamId == team.Id)
                .Take(n)
                .ToList();

            if (teamGames.Count < n)
                continue;

            int wins = 0, losses = 0;
            decimal totalScored = 0, totalAllowed = 0;

            foreach (var g in teamGames)
            {
                bool isHome = g.HomeTeamId == team.Id;
                int scored = isHome ? g.HomeScore : g.VisitorScore;
                int allowed = isHome ? g.VisitorScore : g.HomeScore;
                totalScored += scored;
                totalAllowed += allowed;

                if (scored > allowed) wins++;
                else losses++;
            }

            var windowWinPct = (decimal)wins / teamGames.Count;
            var windowPtsScored = totalScored / teamGames.Count;
            var windowPtsAllowed = totalAllowed / teamGames.Count;
            var windowNetRating = windowPtsScored - windowPtsAllowed;

            // Baseline from standings
            standings.TryGetValue(team.Id, out var standing);
            var baselineWinPct = standing?.WinPct ?? 0m;
            var baselineOffRating = standing?.OffRating ?? 0m;
            var baselineDefRating = standing?.DefRating ?? 0m;
            var baselineNetRating = standing?.NetRating ?? 0m;

            var winPctDelta = windowWinPct - baselineWinPct;
            var scoringDelta = windowPtsScored - baselineOffRating;
            var netRatingDelta = windowNetRating - baselineNetRating;

            var heatScore =
                (winPctDelta / 0.15m) * 0.35m +      // ~15% win% swing is significant
                (netRatingDelta / 8m) * 0.30m +       // ~8 pt margin swing
                (scoringDelta / 8m) * 0.20m +          // ~8 pt scoring swing
                ((baselineDefRating - windowPtsAllowed) / 8m) * 0.15m;  // defensive improvement

            result.Add(new HotTeamDto
            {
                Team = _mapper.Map<TeamDto>(team),
                HeatScore = Math.Round(heatScore, 3),
                WindowWins = wins,
                WindowLosses = losses,
                WindowWinPct = Math.Round(windowWinPct, 3),
                WindowPtsScored = Math.Round(windowPtsScored, 1),
                WindowPtsAllowed = Math.Round(windowPtsAllowed, 1),
                WindowNetRating = Math.Round(windowNetRating, 1),
                BaselineWinPct = Math.Round(baselineWinPct, 3),
                BaselineOffRating = Math.Round(baselineOffRating, 1),
                BaselineDefRating = Math.Round(baselineDefRating, 1),
                BaselineNetRating = Math.Round(baselineNetRating, 1),
                WinPctDelta = Math.Round(winPctDelta, 3),
                ScoringDelta = Math.Round(scoringDelta, 1),
                NetRatingDelta = Math.Round(netRatingDelta, 1),
            });
        }

        return result.OrderByDescending(t => t.HeatScore).ToList();
    }

    // ─── Hot Teams: This Season vs Last Season ───

    private async Task<List<HotTeamDto>> ComputeTeamSeasonVsLast(
        int currentSeasonId, int currentSeasonYear, CancellationToken ct)
    {
        var prevSeason = await _db.Seasons.FirstOrDefaultAsync(s => s.Year == currentSeasonYear - 1, ct);
        if (prevSeason == null)
            return [];

        // Current standings
        var currentDate = await _db.StandingsSnapshots
            .Where(s => s.SeasonId == currentSeasonId)
            .MaxAsync(s => (DateTime?)s.SnapshotDate, ct);

        var currentStandings = currentDate != null
            ? await _db.StandingsSnapshots
                .Where(s => s.SeasonId == currentSeasonId && s.SnapshotDate == currentDate.Value)
                .Include(s => s.Team)
                .ToListAsync(ct)
            : new List<NbaDashboard.Core.Entities.StandingsSnapshot>();

        // Previous season's latest standings
        var prevDate = await _db.StandingsSnapshots
            .Where(s => s.SeasonId == prevSeason.Id)
            .MaxAsync(s => (DateTime?)s.SnapshotDate, ct);

        var prevStandings = prevDate != null
            ? await _db.StandingsSnapshots
                .Where(s => s.SeasonId == prevSeason.Id && s.SnapshotDate == prevDate.Value)
                .ToDictionaryAsync(s => s.TeamId, ct)
            : new Dictionary<int, NbaDashboard.Core.Entities.StandingsSnapshot>();

        var result = new List<HotTeamDto>();

        foreach (var current in currentStandings)
        {
            if (!prevStandings.TryGetValue(current.TeamId, out var prev))
                continue;

            var winPctDelta = current.WinPct - prev.WinPct;
            var offDelta = current.OffRating - prev.OffRating;
            var defDelta = prev.DefRating - current.DefRating; // lower is better
            var netRatingDelta = current.NetRating - prev.NetRating;

            var heatScore =
                (winPctDelta / 0.15m) * 0.35m +
                (netRatingDelta / 8m) * 0.30m +
                (offDelta / 8m) * 0.20m +
                (defDelta / 8m) * 0.15m;

            result.Add(new HotTeamDto
            {
                Team = _mapper.Map<TeamDto>(current.Team),
                HeatScore = Math.Round(heatScore, 3),
                WindowWins = current.Wins,
                WindowLosses = current.Losses,
                WindowWinPct = Math.Round(current.WinPct, 3),
                WindowPtsScored = Math.Round(current.OffRating, 1),
                WindowPtsAllowed = Math.Round(current.DefRating, 1),
                WindowNetRating = Math.Round(current.NetRating, 1),
                BaselineWinPct = Math.Round(prev.WinPct, 3),
                BaselineOffRating = Math.Round(prev.OffRating, 1),
                BaselineDefRating = Math.Round(prev.DefRating, 1),
                BaselineNetRating = Math.Round(prev.NetRating, 1),
                WinPctDelta = Math.Round(winPctDelta, 3),
                ScoringDelta = Math.Round(current.OffRating - prev.OffRating, 1),
                NetRatingDelta = Math.Round(netRatingDelta, 1),
            });
        }

        return result.OrderByDescending(t => t.HeatScore).ToList();
    }
}
