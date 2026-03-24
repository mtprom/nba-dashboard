using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
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
        // Clear scoreboard cache between tests to prevent cross-test interference
        factory.Services.GetRequiredService<IMemoryCache>().Remove("scoreboard_today");
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

    [Fact]
    public async Task GetUpcoming_TeamsNotInDb_FallsBackToLineScoreData()
    {
        // Use team IDs that are NOT seeded in the test database
        const int hawksId = 1610612737;
        const int bullsId = 1610612741;

        _fakeNba.Setup(() => new ScoreboardV2Response
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
                            JsonElement("0022500888"),
                            JsonElement(1),
                            JsonElement("8:00 PM ET"),
                            JsonElement(hawksId),
                            JsonElement(bullsId),
                            JsonElement("State Farm Arena"),
                        ]
                    ]
                },
                new ScoreboardResultSet
                {
                    Name = "LineScore",
                    Headers = ["GAME_DATE_EST", "GAME_SEQUENCE", "GAME_ID", "TEAM_ID",
                        "TEAM_ABBREVIATION", "TEAM_CITY_NAME", "TEAM_NICKNAME", "PTS"],
                    RowSet =
                    [
                        [
                            JsonElement("2026-03-17T00:00:00"),
                            JsonElement(1),
                            JsonElement("0022500888"),
                            JsonElement(hawksId),
                            JsonElement("ATL"),
                            JsonElement("Atlanta"),
                            JsonElement("Hawks"),
                            JsonElement(0),
                        ],
                        [
                            JsonElement("2026-03-17T00:00:00"),
                            JsonElement(1),
                            JsonElement("0022500888"),
                            JsonElement(bullsId),
                            JsonElement("CHI"),
                            JsonElement("Chicago"),
                            JsonElement("Bulls"),
                            JsonElement(0),
                        ]
                    ]
                }
            ]
        });

        var resp = await _client.GetAsync("/api/games/upcoming");

        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<List<UpcomingGameDto>>();

        Assert.NotNull(result);
        Assert.Single(result);

        var game = result[0];
        Assert.Equal("ATL", game.HomeTeam.Abbreviation);
        Assert.Equal("Hawks", game.HomeTeam.Name);
        Assert.Equal("Atlanta", game.HomeTeam.City);
        Assert.Equal("CHI", game.VisitorTeam.Abbreviation);
        Assert.Equal("Bulls", game.VisitorTeam.Name);
        Assert.Equal("Chicago", game.VisitorTeam.City);
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
                    Headers = ["GAME_DATE_EST", "GAME_SEQUENCE", "GAME_ID", "TEAM_ID",
                        "TEAM_ABBREVIATION", "TEAM_CITY_NAME", "TEAM_NICKNAME", "PTS"],
                    RowSet =
                    [
                        [
                            JsonElement("2026-03-17T00:00:00"),
                            JsonElement(1),
                            JsonElement("0022500999"),
                            JsonElement(TestDataSeeder.CelticsId),
                            JsonElement("BOS"),
                            JsonElement("Boston"),
                            JsonElement("Celtics"),
                            JsonElement(0),
                        ],
                        [
                            JsonElement("2026-03-17T00:00:00"),
                            JsonElement(1),
                            JsonElement("0022500999"),
                            JsonElement(TestDataSeeder.LakersId),
                            JsonElement("LAL"),
                            JsonElement("Los Angeles"),
                            JsonElement("Lakers"),
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
