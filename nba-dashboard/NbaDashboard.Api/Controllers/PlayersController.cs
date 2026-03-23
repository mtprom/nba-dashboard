using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NbaDashboard.Api.DTOs;
using NbaDashboard.Infrastructure.Data;

namespace NbaDashboard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlayersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;
    private readonly ILogger<PlayersController> _logger;

    public PlayersController(AppDbContext db, IMapper mapper, ILogger<PlayersController> logger)
    {
        _db = db;
        _mapper = mapper;
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
            .ToList();

        if (ids.Count == 0)
            return BadRequest("No valid team IDs provided");

        try
        {
            var now = DateTime.UtcNow;
            int seasonYear = now.Month >= 10 ? now.Year : now.Year - 1;
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

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load season averages for teams {TeamIds}", teamIds);
            return StatusCode(500, new { error = "Failed to load season averages" });
        }
    }
}
