using System.Net;
using System.Net.Http.Json;
using NbaDashboard.Api.DTOs;
using NbaDashboard.Api.Tests.Fixtures;
using NbaDashboard.Api.Tests.Helpers;

namespace NbaDashboard.Api.Tests;

public class TeamsControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TeamsControllerTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetMatchup_ValidTeams_ReturnsHistory()
    {
        var resp = await _client.GetAsync(
            $"/api/teams/{TestDataSeeder.CelticsId}/matchup/{TestDataSeeder.LakersId}");

        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<MatchupHistoryDto>();

        Assert.NotNull(result);
        Assert.Equal(5, result.Games.Count);
        Assert.Equal("Celtics", result.Team.Name);
        Assert.Equal("Lakers", result.Opponent.Name);
        Assert.Equal(3, result.TeamWins);
        Assert.Equal(2, result.OpponentWins);
    }

    [Fact]
    public async Task GetMatchup_ValidTeams_ReturnsPlayerStats()
    {
        var resp = await _client.GetAsync(
            $"/api/teams/{TestDataSeeder.CelticsId}/matchup/{TestDataSeeder.LakersId}");

        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<MatchupHistoryDto>();

        Assert.NotNull(result);
        var firstGame = result.Games[0]; // Most recent game

        Assert.True(firstGame.HomePlayerStats.Count > 0);
        Assert.True(firstGame.VisitorPlayerStats.Count > 0);

        // Verify player names are populated
        var allNames = firstGame.HomePlayerStats
            .Concat(firstGame.VisitorPlayerStats)
            .Select(p => p.PlayerName)
            .ToList();

        Assert.Contains("Jayson Tatum", allNames);
        Assert.Contains("LeBron James", allNames);
    }

    [Fact]
    public async Task GetMatchup_ReversedTeamOrder_ReturnsSameGamesWithSwappedWins()
    {
        var resp = await _client.GetAsync(
            $"/api/teams/{TestDataSeeder.LakersId}/matchup/{TestDataSeeder.CelticsId}");

        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<MatchupHistoryDto>();

        Assert.NotNull(result);
        Assert.Equal(5, result.Games.Count);
        Assert.Equal("Lakers", result.Team.Name);
        Assert.Equal("Celtics", result.Opponent.Name);
        Assert.Equal(2, result.TeamWins);   // Lakers won 2
        Assert.Equal(3, result.OpponentWins); // Celtics won 3
    }

    [Fact]
    public async Task GetMatchup_NoHistory_ReturnsEmptyGames()
    {
        var resp = await _client.GetAsync(
            $"/api/teams/{TestDataSeeder.ThunderId}/matchup/{TestDataSeeder.CelticsId}");

        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<MatchupHistoryDto>();

        Assert.NotNull(result);
        Assert.Empty(result.Games);
        Assert.Equal(0, result.TeamWins);
        Assert.Equal(0, result.OpponentWins);
    }

    [Fact]
    public async Task GetMatchup_InvalidTeamId_ReturnsNotFound()
    {
        var resp = await _client.GetAsync("/api/teams/9999999/matchup/9999998");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }
}
