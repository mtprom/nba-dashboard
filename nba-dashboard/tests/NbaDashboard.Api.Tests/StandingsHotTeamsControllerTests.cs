using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using NbaDashboard.Api.DTOs;
using NbaDashboard.Api.Tests.Fixtures;
using NbaDashboard.Api.Tests.Helpers;
using NbaDashboard.Core.Entities;
using NbaDashboard.Infrastructure.Data;

namespace NbaDashboard.Api.Tests;

public class StandingsHotTeamsControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public StandingsHotTeamsControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetStandings_ReturnsStoredAdvancedRatings()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.StandingsSnapshots.RemoveRange(db.StandingsSnapshots);
        db.SaveChanges();

        db.StandingsSnapshots.AddRange(
            new StandingsSnapshot
            {
                TeamId = TestDataSeeder.CelticsId,
                SeasonId = 1,
                SnapshotDate = new DateTime(2026, 4, 8, 0, 0, 0, DateTimeKind.Utc),
                Wins = 57,
                Losses = 22,
                WinPct = 0.722m,
                ConfRank = 1,
                DivRank = 1,
                HomeRecord = "30-9",
                AwayRecord = "27-13",
                Last10 = "7-3",
                Streak = "W 3",
                OffRating = 118.7m,
                DefRating = 109.4m,
                NetRating = 9.3m,
                Pace = 98.6m,
            },
            new StandingsSnapshot
            {
                TeamId = TestDataSeeder.LakersId,
                SeasonId = 1,
                SnapshotDate = new DateTime(2026, 4, 8, 0, 0, 0, DateTimeKind.Utc),
                Wins = 48,
                Losses = 31,
                WinPct = 0.608m,
                ConfRank = 4,
                DivRank = 2,
                HomeRecord = "28-12",
                AwayRecord = "20-19",
                Last10 = "6-4",
                Streak = "W 1",
                OffRating = 115.2m,
                DefRating = 112.4m,
                NetRating = 2.8m,
                Pace = 100.1m,
            });
        db.SaveChanges();

        var response = await _client.GetAsync("/api/standings?season=2025");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<StandingsDto>>();

        Assert.NotNull(result);
        var celtics = Assert.Single(result, s => s.Team.Id == TestDataSeeder.CelticsId);
        Assert.Equal(118.7m, celtics.OffRating);
        Assert.Equal(109.4m, celtics.DefRating);
        Assert.Equal(9.3m, celtics.NetRating);
        Assert.Equal(98.6m, celtics.Pace);
    }

    [Fact]
    public async Task GetHotTeams_SeasonWindow_UsesPreviousSeasonBaselineRatings()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.StandingsSnapshots.RemoveRange(db.StandingsSnapshots);
        var existingPreviousSeason = db.Seasons.SingleOrDefault(s => s.Year == 2024);
        if (existingPreviousSeason == null)
        {
            existingPreviousSeason = new Season { Id = 2, Year = 2024 };
            db.Seasons.Add(existingPreviousSeason);
        }

        db.StandingsSnapshots.AddRange(
            new StandingsSnapshot
            {
                TeamId = TestDataSeeder.CelticsId,
                SeasonId = 1,
                SnapshotDate = new DateTime(2026, 4, 8, 0, 0, 0, DateTimeKind.Utc),
                Wins = 57,
                Losses = 22,
                WinPct = 0.722m,
                ConfRank = 1,
                DivRank = 1,
                HomeRecord = "30-9",
                AwayRecord = "27-13",
                Last10 = "7-3",
                Streak = "W 3",
                OffRating = 119.1m,
                DefRating = 110.4m,
                NetRating = 8.7m,
                Pace = 98.4m,
            },
            new StandingsSnapshot
            {
                TeamId = TestDataSeeder.LakersId,
                SeasonId = 1,
                SnapshotDate = new DateTime(2026, 4, 8, 0, 0, 0, DateTimeKind.Utc),
                Wins = 45,
                Losses = 34,
                WinPct = 0.570m,
                ConfRank = 6,
                DivRank = 3,
                HomeRecord = "24-16",
                AwayRecord = "21-18",
                Last10 = "5-5",
                Streak = "L 1",
                OffRating = 113.0m,
                DefRating = 114.2m,
                NetRating = -1.2m,
                Pace = 99.5m,
            },
            new StandingsSnapshot
            {
                TeamId = TestDataSeeder.CelticsId,
                SeasonId = existingPreviousSeason.Id,
                SnapshotDate = new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Utc),
                Wins = 64,
                Losses = 18,
                WinPct = 0.780m,
                ConfRank = 1,
                DivRank = 1,
                HomeRecord = "34-7",
                AwayRecord = "30-11",
                Last10 = "6-4",
                Streak = "L 1",
                OffRating = 117.4m,
                DefRating = 111.2m,
                NetRating = 6.2m,
                Pace = 97.9m,
            },
            new StandingsSnapshot
            {
                TeamId = TestDataSeeder.LakersId,
                SeasonId = existingPreviousSeason.Id,
                SnapshotDate = new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Utc),
                Wins = 47,
                Losses = 35,
                WinPct = 0.573m,
                ConfRank = 7,
                DivRank = 3,
                HomeRecord = "27-14",
                AwayRecord = "20-21",
                Last10 = "4-6",
                Streak = "L 2",
                OffRating = 112.1m,
                DefRating = 113.5m,
                NetRating = -1.4m,
                Pace = 99.0m,
            });
        db.SaveChanges();

        var response = await _client.GetAsync("/api/hot/teams?window=season");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<HotTeamsResponseDto>();

        Assert.NotNull(result);
        var allTeams = result.Hot.Concat(result.Cold).ToList();
        var celtics = Assert.Single(allTeams, t => t.Team.Id == TestDataSeeder.CelticsId);
        Assert.Equal(117.4m, celtics.BaselineOffRating);
        Assert.Equal(111.2m, celtics.BaselineDefRating);
        Assert.Equal(6.2m, celtics.BaselineNetRating);
    }
}
