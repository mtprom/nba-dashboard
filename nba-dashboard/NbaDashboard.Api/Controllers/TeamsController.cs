using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NbaDashboard.Api.DTOs;
using NbaDashboard.Infrastructure.Data;

namespace NbaDashboard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeamsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;
    private readonly ILogger<TeamsController> _logger;

    public TeamsController(AppDbContext db, IMapper mapper, ILogger<TeamsController> logger)
    {
        _db = db;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpGet("{teamId:int}/matchup/{opponentId:int}")]
    public async Task<ActionResult<MatchupHistoryDto>> GetMatchupHistory(
        int teamId, int opponentId, CancellationToken ct)
    {
        try
        {
            var team = await _db.Teams.FindAsync([teamId], ct);
            var opponent = await _db.Teams.FindAsync([opponentId], ct);
            if (team == null || opponent == null)
                return NotFound();

            var games = await _db.Games
                .Where(g => g.Status == "Final"
                    && ((g.HomeTeamId == teamId && g.VisitorTeamId == opponentId)
                        || (g.HomeTeamId == opponentId && g.VisitorTeamId == teamId)))
                .OrderByDescending(g => g.Date)
                .Take(5)
                .Include(g => g.HomeTeam)
                .Include(g => g.VisitorTeam)
                .Include(g => g.PlayerGameStats)
                    .ThenInclude(s => s.Player)
                .AsSplitQuery()
                .ToListAsync(ct);

            var matchupGames = games.Select(g => new MatchupGameDto
            {
                Game = _mapper.Map<GameDto>(g),
                HomeTeam = _mapper.Map<TeamDto>(g.HomeTeam),
                VisitorTeam = _mapper.Map<TeamDto>(g.VisitorTeam),
                HomePlayerStats = g.PlayerGameStats
                    .Where(s => s.TeamId == g.HomeTeamId)
                    .Select(s => _mapper.Map<PlayerGameStatsDto>(s))
                    .OrderByDescending(s => s.Minutes)
                    .ToList(),
                VisitorPlayerStats = g.PlayerGameStats
                    .Where(s => s.TeamId == g.VisitorTeamId)
                    .Select(s => _mapper.Map<PlayerGameStatsDto>(s))
                    .OrderByDescending(s => s.Minutes)
                    .ToList(),
            }).ToList();

            int teamWins = games.Count(g =>
                (g.HomeTeamId == teamId && g.HomeScore > g.VisitorScore) ||
                (g.VisitorTeamId == teamId && g.VisitorScore > g.HomeScore));

            return Ok(new MatchupHistoryDto
            {
                Team = _mapper.Map<TeamDto>(team),
                Opponent = _mapper.Map<TeamDto>(opponent),
                Games = matchupGames,
                TeamWins = teamWins,
                OpponentWins = games.Count - teamWins,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load matchup history for teams {TeamId} vs {OpponentId}", teamId, opponentId);
            return StatusCode(500, new { error = "Failed to load matchup history" });
        }
    }
}
