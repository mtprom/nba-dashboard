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

    services.AddHttpClient<NbaStatsClient>().ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();
        NbaStatsHeaders.ConfigureHandler(handler);
        return handler;
    });

    services.AddScoped<SyncBoxScoresJob>();
    services.AddScoped<HistoricalBackfillJob>();

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
    // Run historical backfill once on startup (resumes from cursor if interrupted)
    var backfill = scope.ServiceProvider.GetRequiredService<HistoricalBackfillJob>();
    await backfill.RunAsync();

    // Schedule nightly box score sync at 3:00 AM going forward
    RecurringJob.AddOrUpdate<SyncBoxScoresJob>(
        "sync-box-scores",
        job => job.RunAsync(null, CancellationToken.None),
        "0 3 * * *");
}

host.Run();
