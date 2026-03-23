using System.Net;
using System.Net.Http.Json;
using NbaDashboard.Api.DTOs;
using NbaDashboard.Api.Tests.Fixtures;
using NbaDashboard.Api.Tests.Helpers;

namespace NbaDashboard.Api.Tests;

public class PlayersControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PlayersControllerTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSeasonAverages_ValidTeamIds_ReturnsFilteredPlayers()
    {
        var resp = await _client.GetAsync(
            $"/api/players/season-averages?teamIds={TestDataSeeder.CelticsId}");

        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<Dictionary<int, PlayerSeasonAvgDto>>();

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey(1628369)); // Tatum
        Assert.True(result.ContainsKey(1627759)); // Brown

        var tatum = result[1628369];
        Assert.Equal("Jayson Tatum", tatum.PlayerName);
        Assert.Equal("SF", tatum.Position);
        Assert.Equal("0", tatum.JerseyNumber);
        Assert.Equal(58, tatum.GamesPlayed);
        Assert.Equal(27.1m, tatum.PtsAvg);
    }

    [Fact]
    public async Task GetSeasonAverages_MultipleTeamIds_ReturnsBothTeams()
    {
        var resp = await _client.GetAsync(
            $"/api/players/season-averages?teamIds={TestDataSeeder.CelticsId},{TestDataSeeder.LakersId}");

        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<Dictionary<int, PlayerSeasonAvgDto>>();

        Assert.NotNull(result);
        Assert.Equal(4, result.Count);
        Assert.True(result.ContainsKey(2544)); // LeBron
        Assert.True(result.ContainsKey(203076)); // AD
    }

    [Fact]
    public async Task GetSeasonAverages_MissingTeamIds_ReturnsBadRequest()
    {
        var resp = await _client.GetAsync("/api/players/season-averages");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GetSeasonAverages_InvalidTeamIds_ReturnsBadRequest()
    {
        var resp = await _client.GetAsync("/api/players/season-averages?teamIds=abc");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }
}
