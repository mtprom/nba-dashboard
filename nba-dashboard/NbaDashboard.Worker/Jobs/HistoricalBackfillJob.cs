using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NbaDashboard.Core.Entities;
using NbaDashboard.Infrastructure.Data;

namespace NbaDashboard.Worker.Jobs;

public class HistoricalBackfillJob
{
    private readonly SyncBoxScoresJob _syncJob;
    private readonly AppDbContext _db;
    private readonly ILogger<HistoricalBackfillJob> _logger;
    private readonly int _startSeasonYear;

    public HistoricalBackfillJob(SyncBoxScoresJob syncJob, AppDbContext db,
        ILogger<HistoricalBackfillJob> logger, IConfiguration config)
    {
        _syncJob = syncJob;
        _db = db;
        _logger = logger;

        // Configurable via NbaStats__BackfillStartDate env var; defaults to 2024-25 season open
        var startDate = DateOnly.Parse(config["NbaStats:BackfillStartDate"] ?? "2024-10-22");
        _startSeasonYear = startDate.Month >= 10 ? startDate.Year : startDate.Year - 1;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        int currentSeasonYear = now.Month >= 10 ? now.Year : now.Year - 1;

        _logger.LogInformation("Starting historical backfill: seasons {Start}-{End}",
            $"{_startSeasonYear}-{(_startSeasonYear + 1) % 100:D2}",
            $"{currentSeasonYear}-{(currentSeasonYear + 1) % 100:D2}");

        for (int year = _startSeasonYear; year <= currentSeasonYear; year++)
        {
            ct.ThrowIfCancellationRequested();

            var cursorKey = $"backfill_season_{year}";
            if (await _db.SyncStates.AnyAsync(s => s.Key == cursorKey, ct))
            {
                _logger.LogInformation("Season {Year}-{Next} already backfilled, skipping",
                    year, (year + 1) % 100);
                continue;
            }

            await _syncJob.RunForSeasonAsync(year, ct);

            // Mark completed seasons (not the current one — it may have new games)
            if (year < currentSeasonYear)
            {
                _db.SyncStates.Add(new SyncState
                {
                    Key = cursorKey,
                    Value = "done",
                    UpdatedAt = DateTime.UtcNow,
                });
                await _db.SaveChangesAsync(ct);
            }
        }

        _logger.LogInformation("Historical backfill complete");
    }
}
