using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using NbaDashboard.Infrastructure.Data;
using NbaDashboard.Infrastructure.NbaStats;
using NbaDashboard.Worker.Jobs;

System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((context, services) =>
{
    var connStr = context.Configuration.GetConnectionString("Default")!;

    services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connStr));

    services.AddHttpClient<NbaStatsClient>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(15);
    }).ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();
        NbaStatsHeaders.ConfigureHandler(handler);
        return handler;
    });

    services.AddScoped<SyncBoxScoresJob>();
    services.AddScoped<HistoricalBackfillJob>();
    services.AddScoped<SyncSeasonAveragesJob>();
    services.AddScoped<SyncStandingsJob>();
    services.AddScoped<PreWarmScoreboardJob>();

    services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connStr)));

    services.AddHangfireServer();
});

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    // Run standings replay first on startup so historical season comparisons
    // have populated team advanced metrics across the configured range.
    var standings = scope.ServiceProvider.GetRequiredService<SyncStandingsJob>();
    await standings.RunBackfillRangeAsync();

    // Run season averages backfill (2 API calls per season)
    var seasonAvg = scope.ServiceProvider.GetRequiredService<SyncSeasonAveragesJob>();
    await seasonAvg.RunAsync();

    // Run historical backfill (resumes from cursor if interrupted, can take hours)
    var backfill = scope.ServiceProvider.GetRequiredService<HistoricalBackfillJob>();
    await backfill.RunAsync();

    // Schedule nightly recurring jobs
    RecurringJob.AddOrUpdate<SyncBoxScoresJob>(
        "sync-box-scores",
        job => job.RunAsync(null, CancellationToken.None),
        "0 3 * * *");

    RecurringJob.AddOrUpdate<SyncSeasonAveragesJob>(
        "sync-season-averages",
        job => job.SyncCurrentSeasonAsync(CancellationToken.None),
        "0 4 * * *");

    RecurringJob.AddOrUpdate<SyncStandingsJob>(
        "sync-standings",
        job => job.RunAsync(CancellationToken.None),
        "0 5 * * *");

    RecurringJob.AddOrUpdate<PreWarmScoreboardJob>(
        "prewarm-scoreboard",
        job => job.RunAsync(CancellationToken.None),
        "*/15 * * * *");
}

host.Run();
