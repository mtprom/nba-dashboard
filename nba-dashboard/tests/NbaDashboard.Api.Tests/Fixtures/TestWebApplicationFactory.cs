using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NbaDashboard.Api.Tests.Helpers;
using NbaDashboard.Infrastructure.Data;
using NbaDashboard.Infrastructure.NbaStats;

namespace NbaDashboard.Api.Tests.Fixtures;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public FakeNbaStatsClient FakeNbaClient { get; } = new();

    private readonly SqliteConnection _connection;
    private bool _dbSeeded;

    public TestWebApplicationFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove the real AppDbContext registration
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbDescriptor != null)
                services.Remove(dbDescriptor);

            // Remove the real NbaStatsClient registration
            var nbaDescriptors = services
                .Where(d => d.ServiceType == typeof(NbaStatsClient)
                    || d.ServiceType == typeof(IHttpClientFactory))
                .ToList();
            foreach (var d in nbaDescriptors)
                services.Remove(d);

            // Use SQLite in-memory for tests
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(_connection));

            // Register our fake NbaStatsClient as singleton
            services.AddSingleton(FakeNbaClient);
            services.AddSingleton<NbaStatsClient>(sp => sp.GetRequiredService<FakeNbaStatsClient>());
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        if (!_dbSeeded)
        {
            using var scope = host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
            TestDataSeeder.Seed(db);
            _dbSeeded = true;
        }

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}

public class FakeNbaStatsClient : NbaStatsClient
{
    private Func<string, Dictionary<string, string>?, CancellationToken, Task<object?>>? _handler;
    public int CallCount { get; private set; }

    public FakeNbaStatsClient()
        : base(new HttpClient(), new Microsoft.Extensions.Logging.Abstractions.NullLogger<NbaStatsClient>())
    {
    }

    public void Setup<T>(Func<T> factory)
    {
        _handler = (_, _, _) => Task.FromResult<object?>(factory());
    }

    public void SetupThrow(Exception ex)
    {
        _handler = (_, _, _) => throw ex;
    }

    public override async Task<T?> GetAsync<T>(string endpoint, Dictionary<string, string>? parameters = null,
        CancellationToken ct = default) where T : default
    {
        CallCount++;
        if (_handler != null)
        {
            var result = await _handler(endpoint, parameters, ct);
            return (T?)result;
        }
        return default;
    }
}
