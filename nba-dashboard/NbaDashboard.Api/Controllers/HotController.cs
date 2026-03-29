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
    public async Task<ActionResult<HotPlayersResponseDto>> GetHotPlayers(
        [FromQuery] string window = "5", CancellationToken ct = default)
    {
        var cacheKey = $"hot_players_{window}";
        if (_cache.TryGetValue(cacheKey, out HotPlayersResponseDto? cached))
            return Ok(cached);

        try
        {
            var now = DateTime.UtcNow;
            int currentSeasonYear = now.Month >= 10 ? now.Year : now.Year - 1;
            var currentSeason = await _db.Seasons.FirstOrDefaultAsync(s => s.Year == currentSeasonYear, ct);
            if (currentSeason == null)
                return Ok(new HotPlayersResponseDto());

            HotPlayersResponseDto result;

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
    public async Task<ActionResult<HotTeamsResponseDto>> GetHotTeams(
        [FromQuery] string window = "5", CancellationToken ct = default)
    {
        var cacheKey = $"hot_teams_{window}";
        if (_cache.TryGetValue(cacheKey, out HotTeamsResponseDto? cached))
            return Ok(cached);

        try
        {
            var now = DateTime.UtcNow;
            int currentSeasonYear = now.Month >= 10 ? now.Year : now.Year - 1;
            var currentSeason = await _db.Seasons.FirstOrDefaultAsync(s => s.Year == currentSeasonYear, ct);
            if (currentSeason == null)
                return Ok(new HotTeamsResponseDto());

            HotTeamsResponseDto result;

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

    private async Task<HotPlayersResponseDto> ComputeLastNGames(int seasonId, int n, CancellationToken ct)
    {
        // Get all season averages for current season (baseline)
        var seasonStats = await _db.PlayerSeasonStats
            .Where(s => s.SeasonId == seasonId && s.GamesPlayed >= n)
            .Include(s => s.Player).ThenInclude(p => p.Team)
            .ToListAsync(ct);

        if (seasonStats.Count == 0)
            return new HotPlayersResponseDto();

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
            .Where(s => playerIds.Contains(s.PlayerId) && seasonGameIds.Contains(s.GameId)
                        && s.Minutes >= 2m)
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

            // Raw swing weighted — magnitude matters directly
            var heatScore =
                ptsDelta * 0.30m +                // 1 point delta = 0.30 heat
                (tsPctDelta * 100m) * 0.05m +     // 1% TS delta = 0.05 heat
                astDelta * 0.10m +                 // 1 assist delta = 0.10 heat
                netRatingDelta * 0.03m +           // 1 net rtg delta = 0.03 heat
                rebDelta * 0.08m;                  // 1 rebound delta = 0.08 heat

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

        var ordered = result.OrderByDescending(p => p.HeatScore).ToList();
        return new HotPlayersResponseDto
        {
            Hot = ordered.Where(p => p.HeatScore > 0).Take(25).ToList(),
            Cold = ordered.Where(p => p.HeatScore < 0).OrderBy(p => p.HeatScore).Take(25).ToList(),
        };
    }

    // ─── Hot Players: This Season vs Last Season ───

    private async Task<HotPlayersResponseDto> ComputeSeasonVsLastSeason(
        int currentSeasonId, int currentSeasonYear, CancellationToken ct)
    {
        var prevSeason = await _db.Seasons.FirstOrDefaultAsync(s => s.Year == currentSeasonYear - 1, ct);
        if (prevSeason == null)
            return new HotPlayersResponseDto();

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
                ptsDelta * 0.30m +
                (tsPctDelta * 100m) * 0.05m +
                astDelta * 0.10m +
                netRatingDelta * 0.03m +
                rebDelta * 0.08m;

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

        var ordered = result.OrderByDescending(p => p.HeatScore).ToList();
        return new HotPlayersResponseDto
        {
            Hot = ordered.Where(p => p.HeatScore > 0).Take(25).ToList(),
            Cold = ordered.Where(p => p.HeatScore < 0).OrderBy(p => p.HeatScore).Take(25).ToList(),
        };
    }

    // ─── Hot Teams: Last N Games vs Season ───

    private async Task<HotTeamsResponseDto> ComputeTeamLastNGames(int seasonId, int n, CancellationToken ct)
    {
        var teams = await _db.Teams.Where(t => !string.IsNullOrEmpty(t.Conference)).ToListAsync(ct);
        if (teams.Count == 0)
            return new HotTeamsResponseDto();

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

            var defImprovementDelta = baselineDefRating - windowPtsAllowed;
            var heatScore =
                (winPctDelta * 100m) * 0.06m +        // 1% win rate delta = 0.06 heat
                netRatingDelta * 0.15m +               // 1 net rtg delta = 0.15 heat
                scoringDelta * 0.05m +                 // 1 ppg delta = 0.05 heat
                defImprovementDelta * 0.05m;           // 1 pt less allowed = 0.05 heat

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

        var ordered = result.OrderByDescending(t => t.HeatScore).ToList();
        var mid = ordered.Count / 2;
        return new HotTeamsResponseDto
        {
            Hot = ordered.Take(mid).ToList(),
            Cold = ordered.Skip(mid).Reverse().ToList(),
        };
    }

    // ─── Hot Teams: This Season vs Last Season ───

    private async Task<HotTeamsResponseDto> ComputeTeamSeasonVsLast(
        int currentSeasonId, int currentSeasonYear, CancellationToken ct)
    {
        var prevSeason = await _db.Seasons.FirstOrDefaultAsync(s => s.Year == currentSeasonYear - 1, ct);
        if (prevSeason == null)
            return new HotTeamsResponseDto();

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
                (winPctDelta * 100m) * 0.06m +
                netRatingDelta * 0.15m +
                offDelta * 0.05m +
                defDelta * 0.05m;

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

        var ordered = result.OrderByDescending(t => t.HeatScore).ToList();
        var mid = ordered.Count / 2;
        return new HotTeamsResponseDto
        {
            Hot = ordered.Take(mid).ToList(),
            Cold = ordered.Skip(mid).Reverse().ToList(),
        };
    }
}
