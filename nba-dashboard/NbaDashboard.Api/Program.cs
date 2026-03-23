using Microsoft.EntityFrameworkCore;
using NbaDashboard.Api.DTOs;
using NbaDashboard.Infrastructure.Data;
using NbaDashboard.Infrastructure.NbaStats;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddAutoMapper(typeof(MappingProfile));
builder.Services.AddMemoryCache();

builder.Services.AddHttpClient<NbaStatsClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
}).ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    NbaStatsHeaders.ConfigureHandler(handler);
    return handler;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

// Auto-run migrations on startup (skipped in test environment which uses SQLite)
if (!app.Environment.IsEnvironment("Testing"))
{
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
        logger.LogError(ex, "Database migration failed on startup — app will continue without migrations");
    }
}

app.UseCors("Frontend");
app.MapControllers();

app.Run();

// Make Program accessible to WebApplicationFactory in tests
public partial class Program { }
