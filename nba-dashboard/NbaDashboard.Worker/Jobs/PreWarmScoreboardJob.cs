using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NbaDashboard.Core.Entities;
using NbaDashboard.Infrastructure.Data;
using NbaDashboard.Infrastructure.NbaStats;
using NbaDashboard.Infrastructure.NbaStats.Models;

namespace NbaDashboard.Worker.Jobs;

public class PreWarmScoreboardJob
{
    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
    private readonly NbaStatsClient _nba;
    private readonly AppDbContext _db;
    private readonly ILogger<PreWarmScoreboardJob> _logger;

    public PreWarmScoreboardJob(NbaStatsClient nba, AppDbContext db, ILogger<PreWarmScoreboardJob> logger)
    {
        _nba = nba;
        _db = db;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var todayEt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Eastern);
        var today = DateOnly.FromDateTime(todayEt);
        var dateStr = todayEt.ToString("MM/dd/yyyy");

        _logger.LogInformation("PreWarmScoreboardJob fetching scoreboard for {Date}", today);

        var resp = await _nba.GetAsync<ScoreboardV2Response>("scoreboardv2",
            new Dictionary<string, string>
            {
                ["GameDate"] = dateStr,
                ["LeagueID"] = "00",
                ["DayOffset"] = "0",
            }, ct);

        if (resp == null)
        {
            _logger.LogWarning("PreWarmScoreboardJob received null response for {Date}", today);
            return;
        }

        var json = JsonSerializer.Serialize(resp);

        var existing = await _db.CachedScoreboards.FirstOrDefaultAsync(c => c.Date == today, ct);
        if (existing != null)
        {
            existing.PayloadJson = json;
            existing.FetchedAt = DateTime.UtcNow;
        }
        else
        {
            _db.CachedScoreboards.Add(new CachedScoreboard
            {
                Date = today,
                PayloadJson = json,
                FetchedAt = DateTime.UtcNow,
            });
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("PreWarmScoreboardJob stored scoreboard for {Date}", today);
    }
}
