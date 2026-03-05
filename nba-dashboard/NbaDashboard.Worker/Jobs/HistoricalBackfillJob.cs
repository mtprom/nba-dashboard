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
    private readonly DateOnly _seasonStart;

    public HistoricalBackfillJob(SyncBoxScoresJob syncJob, AppDbContext db,
        ILogger<HistoricalBackfillJob> logger, IConfiguration config)
    {
        _syncJob = syncJob;
        _db = db;
        _logger = logger;

        // Configurable via NbaStats__BackfillStartDate env var; defaults to 2024-25 season open
        var startStr = config["NbaStats:BackfillStartDate"] ?? "2024-10-22";
        _seasonStart = DateOnly.Parse(startStr);
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));

        // Resume from cursor if a previous run was interrupted
        var cursorState = await _db.SyncStates
            .FirstOrDefaultAsync(s => s.Key == "backfill_date_cursor", ct);

        var startDate = cursorState != null
            ? DateOnly.Parse(cursorState.Value).AddDays(1)
            : _seasonStart;

        if (startDate > yesterday)
        {
            _logger.LogInformation("Historical backfill already complete through {Date}", yesterday);
            return;
        }

        _logger.LogInformation("Starting historical backfill from {Start} to {End}",
            startDate, yesterday);

        for (var date = startDate; date <= yesterday; date = date.AddDays(1))
        {
            ct.ThrowIfCancellationRequested();

            _logger.LogInformation("Backfilling {Date}", date);
            await _syncJob.RunAsync(date, ct);

            // Update cursor so we can resume if interrupted
            if (cursorState == null)
            {
                cursorState = new SyncState { Key = "backfill_date_cursor" };
                _db.SyncStates.Add(cursorState);
            }
            cursorState.Value = date.ToString("yyyy-MM-dd");
            cursorState.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        _logger.LogInformation("Historical backfill complete");
    }
}
