using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NbaDashboard.Api.DTOs;
using NbaDashboard.Infrastructure.Data;

namespace NbaDashboard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StandingsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;
    private readonly ILogger<StandingsController> _logger;

    public StandingsController(AppDbContext db, IMapper mapper, ILogger<StandingsController> logger)
    {
        _db = db;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/standings?season=2025
    /// Returns latest standings snapshot for all teams in the given season (defaults to current).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<StandingsDto>>> GetStandings(
        [FromQuery] int? season, CancellationToken ct)
    {
        try
        {
            var now = DateTime.UtcNow;
            int seasonYear = season ?? (now.Month >= 10 ? now.Year : now.Year - 1);

            var seasonEntity = await _db.Seasons
                .FirstOrDefaultAsync(s => s.Year == seasonYear, ct);
            if (seasonEntity == null)
                return Ok(new List<StandingsDto>());

            // Get the latest snapshot date for this season
            var latestDate = await _db.StandingsSnapshots
                .Where(s => s.SeasonId == seasonEntity.Id)
                .MaxAsync(s => (DateTime?)s.SnapshotDate, ct);

            if (latestDate == null)
                return Ok(new List<StandingsDto>());

            // Get all team standings for that latest date
            var snapshots = await _db.StandingsSnapshots
                .Where(s => s.SeasonId == seasonEntity.Id && s.SnapshotDate == latestDate.Value)
                .Include(s => s.Team)
                .OrderBy(s => s.ConfRank)
                .ToListAsync(ct);

            var result = snapshots.Select(s => new StandingsDto
            {
                Team = _mapper.Map<TeamDto>(s.Team),
                Conference = s.Team.Conference,
                Wins = s.Wins,
                Losses = s.Losses,
                WinPct = s.WinPct,
                ConfRank = s.ConfRank,
                DivRank = s.DivRank,
                HomeRecord = s.HomeRecord,
                AwayRecord = s.AwayRecord,
                Last10 = s.Last10,
                Streak = s.Streak,
                OffRating = s.OffRating,
                DefRating = s.DefRating,
                NetRating = s.NetRating,
                Pace = s.Pace,
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load standings");
            return StatusCode(500, new { error = "Failed to load standings" });
        }
    }
}
