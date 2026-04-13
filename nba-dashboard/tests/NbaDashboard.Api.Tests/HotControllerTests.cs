using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using NbaDashboard.Api.DTOs;
using NbaDashboard.Api.Tests.Fixtures;
using NbaDashboard.Api.Tests.Helpers;
using NbaDashboard.Core.Entities;
using NbaDashboard.Infrastructure.Data;

namespace NbaDashboard.Api.Tests;

public class HotControllerTests
{
    [Fact]
    public async Task GetHotPlayers_Last5Window_UsesEarlierGamesThisSeasonBaseline()
    {
        using var factory = new TestWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var currentSeason = await ResetHotDataAsync(db);
        SeedRollingGames(db, currentSeason, totalGames: 10, addShortSamplePlayer: true);
        await db.SaveChangesAsync();

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/hot/players?window=5");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<HotPlayersResponseDto>();
        Assert.NotNull(result);

        var allPlayers = result.Hot.Concat(result.Cold).ToList();
        var tatum = Assert.Single(allPlayers, p => p.PlayerId == 1628369);
        Assert.Equal(30.0m, tatum.PtsAvg);
        Assert.Equal(20.0m, tatum.BaselinePtsAvg);
        Assert.Equal(10.0m, tatum.PtsDelta);
        Assert.Equal(0.500m, tatum.FgPct);
        Assert.Equal(0.400m, tatum.BaselineFgPct);
        Assert.Equal(0.650m, tatum.TsPct);
        Assert.Equal(0.550m, tatum.BaselineTsPct);
        Assert.Equal(15.0m, tatum.NetRating);
        Assert.Equal(5.0m, tatum.BaselineNetRating);
        Assert.DoesNotContain(allPlayers, p => p.PlayerId == 1627759);
    }

    [Fact]
    public async Task GetHotPlayers_Last10Window_RequiresTenEarlierGames()
    {
        using var factory = new TestWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var currentSeason = await ResetHotDataAsync(db);
        SeedRollingGames(db, currentSeason, totalGames: 19);
        await db.SaveChangesAsync();

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/hot/players?window=10");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<HotPlayersResponseDto>();
        Assert.NotNull(result);
        Assert.Empty(result.Hot);
        Assert.Empty(result.Cold);
    }

    [Fact]
    public async Task GetHotPlayers_SeasonWindow_UsesLastSeasonBaseline()
    {
        using var factory = new TestWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var currentSeason = await ResetHotDataAsync(db);
        var previousSeason = new Season { Id = 2, Year = currentSeason.Year - 1 };
        db.Seasons.Add(previousSeason);

        db.PlayerSeasonStats.AddRange(
            new PlayerSeasonStats
            {
                PlayerId = 1628369,
                SeasonId = currentSeason.Id,
                TeamId = TestDataSeeder.CelticsId,
                GamesPlayed = 58,
                PtsAvg = 29.5m,
                RebAvg = 8.1m,
                AstAvg = 6.0m,
                FgPct = 0.488m,
                Fg3Pct = 0.382m,
                FtPct = 0.860m,
                TsPct = 0.618m,
                UsgPct = 31.0m,
                NetRating = 9.8m,
            },
            new PlayerSeasonStats
            {
                PlayerId = 1628369,
                SeasonId = previousSeason.Id,
                TeamId = TestDataSeeder.CelticsId,
                GamesPlayed = 74,
                PtsAvg = 26.3m,
                RebAvg = 8.0m,
                AstAvg = 4.9m,
                FgPct = 0.471m,
                Fg3Pct = 0.376m,
                FtPct = 0.844m,
                TsPct = 0.598m,
                UsgPct = 29.7m,
                NetRating = 7.1m,
            });
        await db.SaveChangesAsync();

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/hot/players?window=season");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<HotPlayersResponseDto>();
        Assert.NotNull(result);

        var tatum = Assert.Single(result.Hot, p => p.PlayerId == 1628369);
        Assert.Equal(26.3m, tatum.BaselinePtsAvg);
        Assert.Equal(0.598m, tatum.BaselineTsPct);
        Assert.Equal(7.1m, tatum.BaselineNetRating);
    }

    [Fact]
    public async Task GetHotTeams_Last5Window_UsesEarlierGamesThisSeasonBaseline()
    {
        using var factory = new TestWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var currentSeason = await ResetHotDataAsync(db);
        SeedRollingGames(db, currentSeason, totalGames: 10);

        db.StandingsSnapshots.Add(new StandingsSnapshot
        {
            TeamId = TestDataSeeder.CelticsId,
            SeasonId = currentSeason.Id,
            SnapshotDate = new DateTime(2026, 4, 8, 0, 0, 0, DateTimeKind.Utc),
            Wins = 55,
            Losses = 25,
            WinPct = 0.688m,
            ConfRank = 1,
            DivRank = 1,
            HomeRecord = "30-10",
            AwayRecord = "25-15",
            Last10 = "8-2",
            Streak = "W 4",
            OffRating = 999m,
            DefRating = 888m,
            NetRating = 777m,
            Pace = 100m,
        });
        await db.SaveChangesAsync();

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/hot/teams?window=5");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<HotTeamsResponseDto>();
        Assert.NotNull(result);

        var allTeams = result.Hot.Concat(result.Cold).ToList();
        var celtics = Assert.Single(allTeams, t => t.Team.Id == TestDataSeeder.CelticsId);
        Assert.Equal(120.0m, celtics.WindowPtsScored);
        Assert.Equal(110.0m, celtics.WindowPtsAllowed);
        Assert.Equal(10.0m, celtics.WindowNetRating);
        Assert.Equal(1.000m, celtics.WindowWinPct);
        Assert.Equal(100.0m, celtics.BaselineOffRating);
        Assert.Equal(105.0m, celtics.BaselineDefRating);
        Assert.Equal(-5.0m, celtics.BaselineNetRating);
        Assert.Equal(0.000m, celtics.BaselineWinPct);
    }

    [Fact]
    public async Task GetHotTeams_Last10Window_RequiresTenEarlierGames()
    {
        using var factory = new TestWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var currentSeason = await ResetHotDataAsync(db);
        SeedRollingGames(db, currentSeason, totalGames: 19);
        await db.SaveChangesAsync();

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/hot/teams?window=10");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<HotTeamsResponseDto>();
        Assert.NotNull(result);
        Assert.Empty(result.Hot);
        Assert.Empty(result.Cold);
    }

    private static async Task<Season> ResetHotDataAsync(AppDbContext db)
    {
        db.PlayerGameAdvanced.RemoveRange(db.PlayerGameAdvanced);
        db.PlayerGameStats.RemoveRange(db.PlayerGameStats);
        db.PlayerSeasonStats.RemoveRange(db.PlayerSeasonStats);
        db.StandingsSnapshots.RemoveRange(db.StandingsSnapshots);
        db.Games.RemoveRange(db.Games);
        db.Seasons.RemoveRange(db.Seasons);
        await db.SaveChangesAsync();

        var currentSeasonYear = DateTime.UtcNow.Month >= 10 ? DateTime.UtcNow.Year : DateTime.UtcNow.Year - 1;
        var currentSeason = new Season { Id = 1, Year = currentSeasonYear };
        db.Seasons.Add(currentSeason);
        await db.SaveChangesAsync();
        return currentSeason;
    }

    private static void SeedRollingGames(AppDbContext db, Season currentSeason, int totalGames, bool addShortSamplePlayer = false)
    {
        for (int i = 0; i < totalGames; i++)
        {
            bool isRecent = i >= totalGames - Math.Min(5, totalGames);
            bool celticsHome = i % 2 == 0;
            var gameId = $"0099{i + 1:D6}";
            var gameDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i);

            var celticsScore = isRecent ? 120 : 100;
            var lakersScore = isRecent ? 110 : 105;

            db.Games.Add(new Game
            {
                Id = gameId,
                SeasonId = currentSeason.Id,
                Date = gameDate,
                Status = "Final",
                HomeTeamId = celticsHome ? TestDataSeeder.CelticsId : TestDataSeeder.LakersId,
                VisitorTeamId = celticsHome ? TestDataSeeder.LakersId : TestDataSeeder.CelticsId,
                HomeScore = celticsHome ? celticsScore : lakersScore,
                VisitorScore = celticsHome ? lakersScore : celticsScore,
                Arena = celticsHome ? "TD Garden" : "Crypto.com Arena",
            });

            var tatumPoints = isRecent ? 30 : 20;
            var tatumRebounds = isRecent ? 8 : 5;
            var tatumAssists = isRecent ? 6 : 4;
            var tatumFgPct = isRecent ? 0.500m : 0.400m;
            var tatumTsPct = isRecent ? 0.650m : 0.550m;
            var tatumNetRating = isRecent ? 15m : 5m;

            db.PlayerGameStats.Add(new PlayerGameStats
            {
                GameId = gameId,
                PlayerId = 1628369,
                TeamId = TestDataSeeder.CelticsId,
                Minutes = 36m,
                Points = tatumPoints,
                Rebounds = tatumRebounds,
                Assists = tatumAssists,
                FieldGoalPct = tatumFgPct,
                ThreePointPct = 0.380m,
                FreeThrowPct = 0.850m,
            });
            db.PlayerGameAdvanced.Add(new PlayerGameAdvanced
            {
                GameId = gameId,
                PlayerId = 1628369,
                TeamId = TestDataSeeder.CelticsId,
                Minutes = 36m,
                TsPct = tatumTsPct,
                NetRating = tatumNetRating,
            });

            if (addShortSamplePlayer && i < totalGames - 1)
            {
                db.PlayerGameStats.Add(new PlayerGameStats
                {
                    GameId = gameId,
                    PlayerId = 1627759,
                    TeamId = TestDataSeeder.CelticsId,
                    Minutes = 34m,
                    Points = 18,
                    Rebounds = 5,
                    Assists = 3,
                    FieldGoalPct = 0.450m,
                    ThreePointPct = 0.360m,
                    FreeThrowPct = 0.800m,
                });
                db.PlayerGameAdvanced.Add(new PlayerGameAdvanced
                {
                    GameId = gameId,
                    PlayerId = 1627759,
                    TeamId = TestDataSeeder.CelticsId,
                    Minutes = 34m,
                    TsPct = 0.570m,
                    NetRating = 6m,
                });
            }
        }
    }
}
