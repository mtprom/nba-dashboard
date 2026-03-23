using System.Net;
using AutoMapper;
using NbaDashboard.Api.DTOs;
using NbaDashboard.Api.Tests.Fixtures;
using NbaDashboard.Api.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NbaDashboard.Infrastructure.Data;
using NbaDashboard.Infrastructure.NbaStats;

namespace NbaDashboard.Api.Tests;

public class StartupTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public StartupTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public void AutoMapper_ConfigurationIsValid()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<MappingProfile>());

        config.AssertConfigurationIsValid();
    }

    [Fact]
    public async Task DI_GamesEndpointResponds()
    {
        _factory.FakeNbaClient.Setup<object?>(() => null);
        var resp = await _client.GetAsync("/api/games/upcoming");
        // Any non-500 response means the controller and its dependencies resolved
        Assert.NotEqual(HttpStatusCode.InternalServerError, resp.StatusCode);
    }

    [Fact]
    public async Task DI_TeamsEndpointResponds()
    {
        var resp = await _client.GetAsync($"/api/teams/{TestDataSeeder.CelticsId}/matchup/{TestDataSeeder.LakersId}");
        Assert.NotEqual(HttpStatusCode.InternalServerError, resp.StatusCode);
    }

    [Fact]
    public async Task DI_PlayersEndpointResponds()
    {
        var resp = await _client.GetAsync($"/api/players/season-averages?teamIds={TestDataSeeder.CelticsId}");
        Assert.NotEqual(HttpStatusCode.InternalServerError, resp.StatusCode);
    }

    [Fact]
    public void DI_AppDbContextResolves()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.NotNull(db);
    }

    [Fact]
    public void DI_NbaStatsClientResolves()
    {
        using var scope = _factory.Services.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<NbaStatsClient>();
        Assert.NotNull(client);
    }

    [Fact]
    public void DI_AutoMapperResolves()
    {
        using var scope = _factory.Services.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        Assert.NotNull(mapper);
    }

    [Fact]
    public async Task Startup_AppRespondsToRequests()
    {
        var resp = await _client.GetAsync("/api/games/upcoming");

        // Should get a response (not a connection error), any 2xx/4xx is fine
        Assert.NotEqual(HttpStatusCode.InternalServerError, resp.StatusCode);
    }

    [Fact]
    public async Task CorsPolicy_AllowsAnyOrigin()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/games/upcoming");
        request.Headers.Add("Origin", "http://some-random-origin.example.com");

        var resp = await _client.SendAsync(request);

        Assert.True(resp.Headers.Contains("Access-Control-Allow-Origin"),
            "CORS should allow any origin");
        var allowOrigin = resp.Headers.GetValues("Access-Control-Allow-Origin").First();
        Assert.Equal("*", allowOrigin);
    }
}
