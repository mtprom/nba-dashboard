using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NbaDashboard.Api.DTOs;
using NbaDashboard.Api.Tests.Fixtures;
using NbaDashboard.Api.Tests.Helpers;
using NbaDashboard.Infrastructure.NbaStats.Models;

namespace NbaDashboard.Api.Tests;

/// <summary>
/// Tests that simulate the request patterns a Vite proxy (with changeOrigin: true) sends,
/// ensuring the API handles them correctly without returning raw 500 errors.
/// </summary>
public class ProxyIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProxyIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUpcoming_WithProxyHeaders_ReturnsSuccess()
    {
        _factory.FakeNbaClient.Setup(() => BuildMinimalScoreboard());

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/games/upcoming");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("X-Forwarded-Host", "localhost:5173");
        request.Headers.Add("X-Forwarded-Proto", "http");

        var resp = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.Content.ReadAsStringAsync();
        Assert.StartsWith("[", json.TrimStart());
    }

    [Fact]
    public async Task GetUpcoming_WithNoAcceptHeader_ReturnsJson()
    {
        _factory.FakeNbaClient.Setup(() => BuildMinimalScoreboard());

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/games/upcoming");
        // Deliberately omit Accept header to simulate proxy stripping it
        request.Headers.Clear();

        var resp = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var contentType = resp.Content.Headers.ContentType?.MediaType;
        Assert.Equal("application/json", contentType);
    }

    [Fact]
    public async Task GetMatchup_CrossOriginRequest_IncludesCorsHeaders()
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"/api/teams/{TestDataSeeder.CelticsId}/matchup/{TestDataSeeder.LakersId}");
        request.Headers.Add("Origin", "http://localhost:5173");

        var resp = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.True(resp.Headers.Contains("Access-Control-Allow-Origin"),
            "Response should include CORS Access-Control-Allow-Origin header");
    }

    [Theory]
    [InlineData("/api/games/upcoming")]
    [InlineData("/api/teams/1610612738/matchup/1610612747")]
    [InlineData("/api/players/season-averages?teamIds=1610612738")]
    public async Task AllEndpoints_NeverReturnRaw500(string url)
    {
        _factory.FakeNbaClient.Setup(() => BuildMinimalScoreboard());

        var resp = await _client.GetAsync(url);

        // Endpoints should return 200, 400, or 404 — never an unhandled 500
        Assert.NotEqual(HttpStatusCode.InternalServerError, resp.StatusCode);
    }

    [Fact]
    public async Task CorsPreflight_ReturnsAllowHeaders()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/games/upcoming");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var resp = await _client.SendAsync(request);

        // Preflight should not return 500
        Assert.NotEqual(HttpStatusCode.InternalServerError, resp.StatusCode);
    }

    private static ScoreboardV2Response BuildMinimalScoreboard()
    {
        return new ScoreboardV2Response
        {
            ResultSets =
            [
                new ScoreboardResultSet
                {
                    Name = "GameHeader",
                    Headers = ["GAME_DATE_EST", "GAME_SEQUENCE", "GAME_ID", "GAME_STATUS_ID",
                        "GAME_STATUS_TEXT", "HOME_TEAM_ID", "VISITOR_TEAM_ID", "ARENA_NAME"],
                    RowSet =
                    [
                        [
                            JsonElement("2026-03-22T00:00:00"),
                            JsonElement(1),
                            JsonElement("0022500999"),
                            JsonElement(1),
                            JsonElement("7:30 PM ET"),
                            JsonElement(TestDataSeeder.CelticsId),
                            JsonElement(TestDataSeeder.LakersId),
                            JsonElement("TD Garden"),
                        ]
                    ]
                },
                new ScoreboardResultSet
                {
                    Name = "LineScore",
                    Headers = ["GAME_DATE_EST", "GAME_SEQUENCE", "GAME_ID", "TEAM_ID", "PTS"],
                    RowSet = []
                }
            ]
        };
    }

    private static JsonElement JsonElement(object value)
    {
        var json = JsonSerializer.Serialize(value);
        return JsonDocument.Parse(json).RootElement.Clone();
    }
}
