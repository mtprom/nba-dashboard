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
        var seasonGameIds = await _db.Games
            .Where(g => g.SeasonId == seasonId && g.Status == "Final")
            .OrderByDescending(g => g.Date)
            .Select(g => g.Id)
            .ToListAsync(ct);

        if (seasonGameIds.Count == 0)
            return new HotPlayersResponseDto();

        var allTradStats = await _db.PlayerGameStats
            .Where(s => seasonGameIds.Contains(s.GameId) && s.Minutes >= 2m)
            .Include(s => s.Player).ThenInclude(p => p.Team)
            .Include(s => s.Game)
            .OrderByDescending(s => s.Game.Date)
            .ToListAsync(ct);

        if (allTradStats.Count == 0)
            return new HotPlayersResponseDto();

        var allAdvStats = await _db.PlayerGameAdvanced
            .Where(s => seasonGameIds.Contains(s.GameId))
            .ToDictionaryAsync(s => (s.PlayerId, s.GameId), ct);

        var result = new List<HotPlayerDto>();

        foreach (var playerGames in allTradStats.GroupBy(s => s.PlayerId))
        {
            ct.ThrowIfCancellationRequested();

            var orderedGames = playerGames.ToList();
            if (orderedGames.Count < n * 2)
                continue;

            var recentGames = orderedGames.Take(n).ToList();
            var baselineGames = orderedGames.Skip(n).ToList();

            if (baselineGames.Count < n)
                continue;

            var currentPlayer = recentGames[0].Player;

            var recentCount = (decimal)recentGames.Count;
            var baselineCount = (decimal)baselineGames.Count;

            var ptsAvg = recentGames.Sum(g => (decimal)g.Points) / recentCount;
            var rebAvg = recentGames.Sum(g => (decimal)g.Rebounds) / recentCount;
            var astAvg = recentGames.Sum(g => (decimal)g.Assists) / recentCount;
            var fgPct = recentGames.Average(g => g.FieldGoalPct);

            var baselinePtsAvg = baselineGames.Sum(g => (decimal)g.Points) / baselineCount;
            var baselineRebAvg = baselineGames.Sum(g => (decimal)g.Rebounds) / baselineCount;
            var baselineAstAvg = baselineGames.Sum(g => (decimal)g.Assists) / baselineCount;
            var baselineFgPct = baselineGames.Average(g => g.FieldGoalPct);

            decimal tsPct = 0m, netRating = 0m;
            int recentAdvCount = 0;
            foreach (var game in recentGames)
            {
                if (allAdvStats.TryGetValue((game.PlayerId, game.GameId), out var adv))
                {
                    tsPct += adv.TsPct;
                    netRating += adv.NetRating;
                    recentAdvCount++;
                }
            }
            if (recentAdvCount > 0)
            {
                tsPct /= recentAdvCount;
                netRating /= recentAdvCount;
            }

            decimal baselineTsPct = 0m, baselineNetRating = 0m;
            int baselineAdvCount = 0;
            foreach (var game in baselineGames)
            {
                if (allAdvStats.TryGetValue((game.PlayerId, game.GameId), out var adv))
                {
                    baselineTsPct += adv.TsPct;
                    baselineNetRating += adv.NetRating;
                    baselineAdvCount++;
                }
            }
            if (baselineAdvCount > 0)
            {
                baselineTsPct /= baselineAdvCount;
                baselineNetRating /= baselineAdvCount;
            }

            var ptsDelta = ptsAvg - baselinePtsAvg;
            var rebDelta = rebAvg - baselineRebAvg;
            var astDelta = astAvg - baselineAstAvg;
            var fgPctDelta = fgPct - baselineFgPct;
            var tsPctDelta = tsPct - baselineTsPct;
            var netRatingDelta = netRating - baselineNetRating;

            var heatScore =
                ptsDelta * 0.30m +
                (tsPctDelta * 100m) * 0.05m +
                astDelta * 0.10m +
                netRatingDelta * 0.03m +
                rebDelta * 0.08m;

            result.Add(new HotPlayerDto
            {
                PlayerId = currentPlayer.Id,
                PlayerName = $"{currentPlayer.FirstName} {currentPlayer.LastName}",
                Position = currentPlayer.Position,
                JerseyNumber = currentPlayer.JerseyNumber,
                Team = _mapper.Map<TeamDto>(currentPlayer.Team),
                HeatScore = Math.Round(heatScore, 3),
                GamesPlayed = recentGames.Count,
                PtsAvg = Math.Round(ptsAvg, 1),
                RebAvg = Math.Round(rebAvg, 1),
                AstAvg = Math.Round(astAvg, 1),
                FgPct = Math.Round(fgPct, 3),
                TsPct = Math.Round(tsPct, 3),
                NetRating = Math.Round(netRating, 1),
                BaselinePtsAvg = Math.Round(baselinePtsAvg, 1),
                BaselineRebAvg = Math.Round(baselineRebAvg, 1),
                BaselineAstAvg = Math.Round(baselineAstAvg, 1),
                BaselineFgPct = Math.Round(baselineFgPct, 3),
                BaselineTsPct = Math.Round(baselineTsPct, 3),
                BaselineNetRating = Math.Round(baselineNetRating, 1),
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
        var allGames = await _db.Games
            .Where(g => g.SeasonId == seasonId && g.Status == "Final")
            .OrderByDescending(g => g.Date)
            .ToListAsync(ct);

        if (allGames.Count == 0)
            return new HotTeamsResponseDto();

        var teamIds = allGames
            .SelectMany(g => new[] { g.HomeTeamId, g.VisitorTeamId })
            .Distinct()
            .ToList();

        var teams = await _db.Teams
            .Where(t => teamIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, ct);

        var result = new List<HotTeamDto>();

        foreach (var (teamId, team) in teams)
        {
            var teamGames = allGames
                .Where(g => g.HomeTeamId == teamId || g.VisitorTeamId == teamId)
                .ToList();

            if (teamGames.Count < n * 2)
                continue;

            var recentGames = teamGames.Take(n).ToList();
            var baselineGames = teamGames.Skip(n).ToList();
            if (baselineGames.Count < n)
                continue;

            var (wins, losses, windowWinPct, windowPtsScored, windowPtsAllowed, windowNetRating) =
                SummarizeTeamWindow(teamId, recentGames);

            var (_, _, baselineWinPct, baselineOffRating, baselineDefRating, baselineNetRating) =
                SummarizeTeamWindow(teamId, baselineGames);

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

    private static (int Wins, int Losses, decimal WinPct, decimal PtsScored, decimal PtsAllowed, decimal NetRating)
        SummarizeTeamWindow(int teamId, List<NbaDashboard.Core.Entities.Game> games)
    {
        int wins = 0;
        int losses = 0;
        decimal totalScored = 0m;
        decimal totalAllowed = 0m;

        foreach (var game in games)
        {
            bool isHome = game.HomeTeamId == teamId;
            int scored = isHome ? game.HomeScore : game.VisitorScore;
            int allowed = isHome ? game.VisitorScore : game.HomeScore;
            totalScored += scored;
            totalAllowed += allowed;

            if (scored > allowed)
                wins++;
            else
                losses++;
        }

        var count = games.Count;
        var winPct = count > 0 ? (decimal)wins / count : 0m;
        var ptsScored = count > 0 ? totalScored / count : 0m;
        var ptsAllowed = count > 0 ? totalAllowed / count : 0m;
        return (wins, losses, winPct, ptsScored, ptsAllowed, ptsScored - ptsAllowed);
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
