using Microsoft.EntityFrameworkCore;
using NbaDashboard.Infrastructure.Data;
using NbaDashboard.Infrastructure.NbaStats;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((context, services) =>
{
    services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(context.Configuration.GetConnectionString("Default")));

    services.AddHttpClient<NbaStatsClient>();
});

var host = builder.Build();
host.Run();
