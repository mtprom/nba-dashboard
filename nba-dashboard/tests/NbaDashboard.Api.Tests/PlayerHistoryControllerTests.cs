using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using NbaDashboard.Api.DTOs;
using NbaDashboard.Api.Tests.Fixtures;
using NbaDashboard.Api.Tests.Helpers;
using NbaDashboard.Core.Entities;
using NbaDashboard.Infrastructure.Data;

namespace NbaDashboard.Api.Tests;

public class PlayerHistoryControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PlayerHistoryControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetPlayerHistory_InvalidSeasonRange_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/players/history?playerId=1628369&fromSeason=2025&toSeason=2024");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetPlayerHistory_UnknownPlayer_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/players/history?playerId=999999&fromSeason=2025&toSeason=2025");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPlayerHistory_ExcludesInvalidFinalRows()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tatum = db.Players.Single(p => p.Id == 1628369);

        db.Games.Add(new Game
        {
            Id = "0022599911",
            SeasonId = 1,
            Date = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
            Status = "Final",
            HomeTeamId = TestDataSeeder.CelticsId,
            VisitorTeamId = TestDataSeeder.LakersId,
            HomeScore = 0,
            VisitorScore = 0,
        });

        db.PlayerGameStats.Add(new PlayerGameStats
        {
            GameId = "0022599911",
            PlayerId = tatum.Id,
            TeamId = TestDataSeeder.CelticsId,
            StartPosition = tatum.Position,
            Minutes = 35,
            Points = 44,
            Rebounds = 9,
            Assists = 6,
            FieldGoalsMade = 16,
            FieldGoalsAttempted = 24,
            FieldGoalPct = 0.667m,
        });

        db.SaveChanges();

        var response = await _client.GetAsync("/api/players/history?playerId=1628369&fromSeason=2025&toSeason=2025");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PlayerHistoryResponseDto>();

        Assert.NotNull(result);
        Assert.Equal(5, result.Metrics.TotalGames);
        Assert.DoesNotContain(result.GameLog, g => g.GameId == "0022599911");
    }

    [Fact]
    public async Task GetPlayerHistory_ReturnsSeasonAggregatesSplitsAndHighlights()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tatum = db.Players.Single(p => p.Id == 1628369);

        var season = new Season { Id = 2, Year = 2024 };
        db.Seasons.Add(season);

        db.Games.Add(new Game
        {
            Id = "0022409001",
            SeasonId = season.Id,
            Date = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            Status = "Final",
            HomeTeamId = TestDataSeeder.ThunderId,
            VisitorTeamId = TestDataSeeder.CelticsId,
            HomeScore = 104,
            VisitorScore = 118,
            Arena = "Paycom Center",
        });

        AddPlayerGame(db, "0022409001", tatum.Id, TestDataSeeder.CelticsId, false, 40, 12, 7, 14, 22, 4, 8, 8, 10);
        db.SaveChanges();

        var response = await _client.GetAsync("/api/players/history?playerId=1628369&fromSeason=2024&toSeason=2025");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PlayerHistoryResponseDto>();

        Assert.NotNull(result);
        Assert.Equal(new[] { 2024, 2025 }, result.AvailableSeasonYears);
        Assert.Equal(6, result.Metrics.TotalGames);
        Assert.Equal(2, result.Metrics.SeasonsCovered);
        Assert.Equal(2, result.SeasonStats.Count);

        var season2024 = Assert.Single(result.SeasonStats, s => s.SeasonYear == 2024);
        Assert.Equal(1, season2024.GamesPlayed);
        Assert.Equal(40.0, season2024.Points);
        Assert.Equal(0.636, season2024.FieldGoalPct);

        var homeSplit = Assert.Single(result.HomeAwaySplits, s => s.Key == "home");
        var awaySplit = Assert.Single(result.HomeAwaySplits, s => s.Key == "away");
        Assert.Equal(3, homeSplit.GamesPlayed);
        Assert.Equal(3, awaySplit.GamesPlayed);

        var winsSplit = Assert.Single(result.WinLossSplits, s => s.Key == "wins");
        var lossesSplit = Assert.Single(result.WinLossSplits, s => s.Key == "losses");
        Assert.Equal(4, winsSplit.GamesPlayed);
        Assert.Equal(2, lossesSplit.GamesPlayed);

        Assert.NotNull(result.Highlights.HighestPoints);
        Assert.Equal("0022409001", result.Highlights.HighestPoints!.GameId);
        Assert.Equal(40, result.Highlights.HighestPoints.Points);
        Assert.NotNull(result.Highlights.BestEfficiency);
        Assert.NotNull(result.Highlights.BestPlusMinus);
        Assert.NotNull(result.Highlights.WorstPlusMinus);
    }

    [Fact]
    public async Task SearchPlayers_ReturnsSeededPlayersWithRangeMetadata()
    {
        var response = await _client.GetAsync("/api/players/search?query=tatum");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<PlayerSearchResultDto>>();

        Assert.NotNull(result);
        var tatum = Assert.Single(result, p => p.PlayerId == 1628369);
        Assert.Equal("Jayson Tatum", tatum.PlayerName);
        Assert.True(tatum.FirstSeasonYear <= tatum.LastSeasonYear);
        Assert.True(tatum.LastSeasonYear >= 2025);
        Assert.True(tatum.GamesPlayed >= 5);
    }

    private static void AddPlayerGame(
        AppDbContext db,
        string gameId,
        int playerId,
        int teamId,
        bool isHome,
        int points,
        int rebounds,
        int assists,
        int fgMade,
        int fgAttempts,
        int threeMade,
        int threeAttempts,
        int ftMade,
        int ftAttempts)
    {
        db.PlayerGameStats.Add(new PlayerGameStats
        {
            GameId = gameId,
            PlayerId = playerId,
            TeamId = teamId,
            StartPosition = "SF",
            Minutes = 39,
            Points = points,
            Rebounds = rebounds,
            Assists = assists,
            Steals = 2,
            Blocks = 1,
            Turnovers = 3,
            PersonalFouls = 2,
            PlusMinus = isHome ? 8 : 14,
            FieldGoalsMade = fgMade,
            FieldGoalsAttempted = fgAttempts,
            FieldGoalPct = fgAttempts > 0 ? Math.Round((decimal)fgMade / fgAttempts, 3) : 0,
            ThreePointersMade = threeMade,
            ThreePointersAttempted = threeAttempts,
            ThreePointPct = threeAttempts > 0 ? Math.Round((decimal)threeMade / threeAttempts, 3) : 0,
            FreeThrowsMade = ftMade,
            FreeThrowsAttempted = ftAttempts,
            FreeThrowPct = ftAttempts > 0 ? Math.Round((decimal)ftMade / ftAttempts, 3) : 0,
            OffensiveRebounds = 3,
            DefensiveRebounds = Math.Max(0, rebounds - 3),
        });

        db.PlayerGameAdvanced.Add(new PlayerGameAdvanced
        {
            GameId = gameId,
            PlayerId = playerId,
            TeamId = teamId,
            Minutes = 39,
            OffRating = 126,
            DefRating = 101,
            NetRating = 25,
            AstPct = 0.281m,
            OrebPct = 0.091m,
            DrebPct = 0.214m,
            RebPct = 0.163m,
            EfgPct = 0.727m,
            TsPct = 0.742m,
            UsgPct = 0.332m,
            Pace = 100.1m,
            Pie = 0.198m,
        });
    }
}
