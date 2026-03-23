using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NbaDashboard.Api.DTOs;
using NbaDashboard.Api.Tests.Fixtures;
using NbaDashboard.Api.Tests.Helpers;
using NbaDashboard.Infrastructure.NbaStats.Models;

namespace NbaDashboard.Api.Tests;

public class ErrorHandlingTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FakeNbaStatsClient _fakeNba;

    public ErrorHandlingTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _fakeNba = factory.FakeNbaClient;
    }

    [Fact]
    public async Task GetUpcoming_NbaApiThrows_Returns200WithEmptyArray()
    {
        _fakeNba.SetupThrow(new HttpRequestException("NBA API unreachable"));

        var resp = await _client.GetAsync("/api/games/upcoming");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var result = await resp.Content.ReadFromJsonAsync<List<UpcomingGameDto>>();
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUpcoming_NbaApiReturnsNull_Returns200WithEmptyArray()
    {
        _fakeNba.Setup<ScoreboardV2Response?>(() => null);

        var resp = await _client.GetAsync("/api/games/upcoming");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var result = await resp.Content.ReadFromJsonAsync<List<UpcomingGameDto>>();
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetMatchup_ValidTeams_Returns200()
    {
        var resp = await _client.GetAsync(
            $"/api/teams/{TestDataSeeder.CelticsId}/matchup/{TestDataSeeder.LakersId}");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var result = await resp.Content.ReadFromJsonAsync<MatchupHistoryDto>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetMatchup_InvalidTeam_Returns404NotRaw500()
    {
        var resp = await _client.GetAsync("/api/teams/9999999/matchup/9999998");

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task GetSeasonAverages_ValidTeams_Returns200()
    {
        var resp = await _client.GetAsync(
            $"/api/players/season-averages?teamIds={TestDataSeeder.CelticsId}");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var result = await resp.Content.ReadFromJsonAsync<Dictionary<int, PlayerSeasonAvgDto>>();
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetSeasonAverages_MissingParam_Returns400NotRaw500()
    {
        var resp = await _client.GetAsync("/api/players/season-averages");

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GetSeasonAverages_NonexistentTeam_Returns200WithEmptyDict()
    {
        var resp = await _client.GetAsync("/api/players/season-averages?teamIds=9999999");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var result = await resp.Content.ReadFromJsonAsync<Dictionary<int, PlayerSeasonAvgDto>>();
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetMatchup_NoGamesExist_Returns200WithEmptyGames()
    {
        var resp = await _client.GetAsync(
            $"/api/teams/{TestDataSeeder.ThunderId}/matchup/{TestDataSeeder.CelticsId}");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var result = await resp.Content.ReadFromJsonAsync<MatchupHistoryDto>();
        Assert.NotNull(result);
        Assert.Empty(result.Games);
        Assert.Equal(0, result.TeamWins);
        Assert.Equal(0, result.OpponentWins);
    }
}
