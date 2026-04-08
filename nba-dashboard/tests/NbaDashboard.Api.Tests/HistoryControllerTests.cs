using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using NbaDashboard.Api.DTOs;
using NbaDashboard.Api.Tests.Fixtures;
using NbaDashboard.Api.Tests.Helpers;
using NbaDashboard.Core.Entities;
using NbaDashboard.Infrastructure.Data;

namespace NbaDashboard.Api.Tests;

public class HistoryControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public HistoryControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHistory_TeamMode_HomeAndAwayWinPct_AddToOne()
    {
        var resp = await _client.GetAsync(
            $"/api/history?teamId={TestDataSeeder.CelticsId}&fromSeason=2025&toSeason=2025");

        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<HistoryResponseDto>();

        Assert.NotNull(result);
        var season = Assert.Single(result.SeasonStats);

        Assert.Equal(0.6, season.WinPct);
        Assert.Equal(0.667, season.HomeWinPct);
        Assert.Equal(0.333, season.AwayWinPct);
        Assert.Equal(1.0, Math.Round(season.HomeWinPct!.Value + season.AwayWinPct!.Value, 3));
    }

    [Fact]
    public async Task GetHistory_TeamMode_ZeroWins_ReturnsZeroHomeAndAwayWinPct()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var season = new Season { Id = 2, Year = 2024 };
        db.Seasons.Add(season);

        db.Games.AddRange(
            new Game
            {
                Id = "0022409001",
                SeasonId = season.Id,
                Date = new DateTime(2025, 3, 10, 0, 0, 0, DateTimeKind.Utc),
                Status = "Final",
                HomeTeamId = TestDataSeeder.ThunderId,
                VisitorTeamId = TestDataSeeder.CelticsId,
                HomeScore = 111,
                VisitorScore = 101,
            },
            new Game
            {
                Id = "0022409002",
                SeasonId = season.Id,
                Date = new DateTime(2025, 3, 12, 0, 0, 0, DateTimeKind.Utc),
                Status = "Final",
                HomeTeamId = TestDataSeeder.CelticsId,
                VisitorTeamId = TestDataSeeder.ThunderId,
                HomeScore = 95,
                VisitorScore = 104,
            });

        db.SaveChanges();

        var resp = await _client.GetAsync(
            $"/api/history?teamId={TestDataSeeder.ThunderId}&fromSeason=2024&toSeason=2024");

        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<HistoryResponseDto>();

        Assert.NotNull(result);
        var seasonStats = Assert.Single(result.SeasonStats);

        Assert.Equal(1.0, seasonStats.WinPct);
        Assert.Equal(0.5, seasonStats.HomeWinPct);
        Assert.Equal(0.5, seasonStats.AwayWinPct);

        var losingResp = await _client.GetAsync(
            $"/api/history?teamId={TestDataSeeder.CelticsId}&fromSeason=2024&toSeason=2024");

        losingResp.EnsureSuccessStatusCode();
        var losingResult = await losingResp.Content.ReadFromJsonAsync<HistoryResponseDto>();

        Assert.NotNull(losingResult);
        var losingSeason = Assert.Single(losingResult.SeasonStats);
        Assert.Equal(0.0, losingSeason.WinPct);
        Assert.Equal(0.0, losingSeason.HomeWinPct);
        Assert.Equal(0.0, losingSeason.AwayWinPct);
    }

    [Fact]
    public async Task GetHistory_ClosestGames_ExcludesFutureAndZeroZeroGames()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Games.Add(new Game
        {
            Id = "0022599999",
            SeasonId = 1,
            Date = new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc),
            Status = "Final",
            HomeTeamId = TestDataSeeder.CelticsId,
            VisitorTeamId = TestDataSeeder.LakersId,
            HomeScore = 0,
            VisitorScore = 0,
        });

        db.SaveChanges();

        var resp = await _client.GetAsync("/api/history?fromSeason=2025&toSeason=2025");

        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<HistoryResponseDto>();

        Assert.NotNull(result);
        Assert.NotEmpty(result.ClosestGames);
        Assert.DoesNotContain(result.ClosestGames, g => g.Id == "0022599999");
        Assert.All(result.ClosestGames, g =>
        {
            Assert.True(g.HomeScore > 0);
            Assert.True(g.AwayScore > 0);
        });
    }

    [Fact]
    public async Task GetHistory_ClosestGames_PopulatesAndOrdersByMarginThenNewestDate()
    {
        var resp = await _client.GetAsync(
            $"/api/history?teamId={TestDataSeeder.CelticsId}&fromSeason=2025&toSeason=2025");

        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<HistoryResponseDto>();

        Assert.NotNull(result);
        Assert.NotEmpty(result.ClosestGames);

        var closest = result.ClosestGames;

        for (var i = 1; i < closest.Count; i++)
        {
            var previousMargin = Math.Abs(closest[i - 1].HomeScore - closest[i - 1].AwayScore);
            var currentMargin = Math.Abs(closest[i].HomeScore - closest[i].AwayScore);

            Assert.True(previousMargin <= currentMargin);

            if (previousMargin == currentMargin)
            {
                Assert.True(string.CompareOrdinal(closest[i - 1].Date, closest[i].Date) >= 0);
            }
        }
    }
}
