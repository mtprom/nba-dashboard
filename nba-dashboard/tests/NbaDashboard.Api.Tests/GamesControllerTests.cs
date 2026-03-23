using System.Net.Http.Json;
using System.Text.Json;
using NbaDashboard.Api.DTOs;
using NbaDashboard.Api.Tests.Fixtures;
using NbaDashboard.Api.Tests.Helpers;
using NbaDashboard.Infrastructure.NbaStats.Models;

namespace NbaDashboard.Api.Tests;

public class GamesControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FakeNbaStatsClient _fakeNba;

    public GamesControllerTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _fakeNba = factory.FakeNbaClient;
    }

    [Fact]
    public async Task GetUpcoming_ReturnsGamesFromScoreboard()
    {
        _fakeNba.Setup(() => BuildFakeScoreboard());

        var resp = await _client.GetAsync("/api/games/upcoming");

        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<List<UpcomingGameDto>>();

        Assert.NotNull(result);
        Assert.Single(result);

        var game = result[0];
        Assert.Equal("0022500999", game.Game.Id);
        Assert.Equal("Scheduled", game.Game.Status);
        Assert.Equal(TestDataSeeder.CelticsId, game.Game.HomeTeamId);
        Assert.Equal(TestDataSeeder.LakersId, game.Game.VisitorTeamId);
        Assert.Equal("Celtics", game.HomeTeam.Name);
        Assert.Equal("Lakers", game.VisitorTeam.Name);
    }

    [Fact]
    public async Task GetUpcoming_NbaApiFailure_ReturnsEmptyArray()
    {
        _fakeNba.SetupThrow(new HttpRequestException("NBA API down"));

        var resp = await _client.GetAsync("/api/games/upcoming");

        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<List<UpcomingGameDto>>();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUpcoming_CachesResponse_SecondCallSkipsApi()
    {
        _fakeNba.Setup(() => BuildFakeScoreboard());
        var callsBefore = _fakeNba.CallCount;

        await _client.GetAsync("/api/games/upcoming");
        await _client.GetAsync("/api/games/upcoming");

        // The fake may have been called from the first test too, so check delta
        var callsDelta = _fakeNba.CallCount - callsBefore;
        Assert.True(callsDelta <= 1, $"Expected at most 1 NBA API call but got {callsDelta}");
    }

    private static ScoreboardV2Response BuildFakeScoreboard()
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
                            JsonElement("2026-03-17T00:00:00"),
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
                    RowSet =
                    [
                        [
                            JsonElement("2026-03-17T00:00:00"),
                            JsonElement(1),
                            JsonElement("0022500999"),
                            JsonElement(TestDataSeeder.CelticsId),
                            JsonElement(0),
                        ],
                        [
                            JsonElement("2026-03-17T00:00:00"),
                            JsonElement(1),
                            JsonElement("0022500999"),
                            JsonElement(TestDataSeeder.LakersId),
                            JsonElement(0),
                        ]
                    ]
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
