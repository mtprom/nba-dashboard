using Microsoft.EntityFrameworkCore;
using NbaDashboard.Infrastructure.Data;
using NbaDashboard.Infrastructure.NbaStats;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
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

// Auto-run migrations on startup (skipped in test environment which uses SQLite)
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.UseCors("Frontend");
app.MapControllers();

app.Run();

// Make Program accessible to WebApplicationFactory in tests
public partial class Program { }
