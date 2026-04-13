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
        Assert.NotNull(result.LeaguePlacement);
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

    [Fact]
    public async Task GetHistory_SingleTeamSingleSeason_ReturnsLeaguePlacementRanks()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        SeedLeaguePlacementSeason(db);

        var resp = await _client.GetAsync(
            $"/api/history?teamId={TestDataSeeder.CelticsId}&fromSeason=2023&toSeason=2023");

        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<HistoryResponseDto>();

        Assert.NotNull(result);
        var placement = result.LeaguePlacement;
        Assert.NotNull(placement);

        Assert.Equal(2023, placement.SeasonYear);
        Assert.Equal(TestDataSeeder.CelticsId, placement.SelectedTeamId);
        Assert.Equal(3, placement.SelectedLeagueRank);
        Assert.Equal(2, placement.SelectedConferenceRank);
        Assert.Equal("East", placement.SelectedConference);

        Assert.Collection(placement.Teams,
            team =>
            {
                Assert.Equal(TestDataSeeder.BullsId, team.TeamId);
                Assert.Equal(1, team.LeagueRank);
                Assert.Equal(1, team.ConferenceRank);
                Assert.Equal(0.75, team.WinPct);
            },
            team =>
            {
                Assert.Equal(TestDataSeeder.ThunderId, team.TeamId);
                Assert.Equal(2, team.LeagueRank);
                Assert.Equal(1, team.ConferenceRank);
                Assert.Equal(0.75, team.WinPct);
            },
            team =>
            {
                Assert.Equal(TestDataSeeder.CelticsId, team.TeamId);
                Assert.Equal(3, team.LeagueRank);
                Assert.Equal(2, team.ConferenceRank);
                Assert.Equal(0.667, team.WinPct);
            },
            team =>
            {
                Assert.Equal(TestDataSeeder.SpursId, team.TeamId);
                Assert.Equal(4, team.LeagueRank);
                Assert.Equal(2, team.ConferenceRank);
                Assert.Equal(0.25, team.WinPct);
            },
            team =>
            {
                Assert.Equal(TestDataSeeder.LakersId, team.TeamId);
                Assert.Equal(5, team.LeagueRank);
                Assert.Equal(3, team.ConferenceRank);
                Assert.Equal(0.2, team.WinPct);
            });
    }

    [Fact]
    public async Task GetHistory_MultiSeasonTeamMode_DoesNotReturnLeaguePlacement()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var season = db.Seasons.SingleOrDefault(s => s.Year == 2024);
        if (season == null)
        {
            season = new Season { Id = 2, Year = 2024 };
            db.Seasons.Add(season);
        }

        if (!db.Games.Any(g => g.Id == "0022409100"))
        {
            db.Games.Add(new Game
            {
                Id = "0022409100",
                SeasonId = season.Id,
                Date = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                Status = "Final",
                HomeTeamId = TestDataSeeder.CelticsId,
                VisitorTeamId = TestDataSeeder.LakersId,
                HomeScore = 108,
                VisitorScore = 100,
            });
            db.SaveChanges();
        }

        var resp = await _client.GetAsync(
            $"/api/history?teamId={TestDataSeeder.CelticsId}&fromSeason=2024&toSeason=2025");

        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<HistoryResponseDto>();

        Assert.NotNull(result);
        Assert.Null(result.LeaguePlacement);
    }

    [Fact]
    public async Task GetHistory_AllTeamsMode_DoesNotReturnLeaguePlacement()
    {
        var resp = await _client.GetAsync("/api/history?fromSeason=2025&toSeason=2025");

        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<HistoryResponseDto>();

        Assert.NotNull(result);
        Assert.Null(result.LeaguePlacement);
    }

    private static void SeedLeaguePlacementSeason(AppDbContext db)
    {
        const int seasonYear = 2023;
        const int seasonId = 23;

        if (!db.Seasons.Any(s => s.Year == seasonYear))
        {
            db.Seasons.Add(new Season { Id = seasonId, Year = seasonYear });
        }

        EnsureTeam(db, TestDataSeeder.BullsId, "Bulls", "Chicago Bulls", "CHI", "Chicago", "East", "Central");
        EnsureTeam(db, TestDataSeeder.SpursId, "Spurs", "San Antonio Spurs", "SAS", "San Antonio", "West", "Southwest");

        var placementGames = new[]
        {
            CreateGame("0022301001", seasonId, TestDataSeeder.CelticsId, TestDataSeeder.LakersId, 118, 110, new DateTime(2024, 1, 5, 0, 0, 0, DateTimeKind.Utc)),
            CreateGame("0022301002", seasonId, TestDataSeeder.CelticsId, TestDataSeeder.ThunderId, 111, 105, new DateTime(2024, 1, 9, 0, 0, 0, DateTimeKind.Utc)),
            CreateGame("0022301003", seasonId, TestDataSeeder.BullsId, TestDataSeeder.CelticsId, 106, 99, new DateTime(2024, 1, 12, 0, 0, 0, DateTimeKind.Utc)),
            CreateGame("0022301004", seasonId, TestDataSeeder.BullsId, TestDataSeeder.LakersId, 120, 112, new DateTime(2024, 1, 17, 0, 0, 0, DateTimeKind.Utc)),
            CreateGame("0022301005", seasonId, TestDataSeeder.BullsId, TestDataSeeder.SpursId, 114, 108, new DateTime(2024, 1, 21, 0, 0, 0, DateTimeKind.Utc)),
            CreateGame("0022301006", seasonId, TestDataSeeder.ThunderId, TestDataSeeder.LakersId, 109, 101, new DateTime(2024, 1, 25, 0, 0, 0, DateTimeKind.Utc)),
            CreateGame("0022301007", seasonId, TestDataSeeder.ThunderId, TestDataSeeder.SpursId, 121, 115, new DateTime(2024, 1, 29, 0, 0, 0, DateTimeKind.Utc)),
            CreateGame("0022301008", seasonId, TestDataSeeder.SpursId, TestDataSeeder.BullsId, 104, 99, new DateTime(2024, 2, 2, 0, 0, 0, DateTimeKind.Utc)),
            CreateGame("0022301009", seasonId, TestDataSeeder.LakersId, TestDataSeeder.SpursId, 117, 111, new DateTime(2024, 2, 7, 0, 0, 0, DateTimeKind.Utc)),
            CreateGame("0022301010", seasonId, TestDataSeeder.LakersId, TestDataSeeder.ThunderId, 98, 103, new DateTime(2024, 2, 11, 0, 0, 0, DateTimeKind.Utc)),
        };

        foreach (var game in placementGames)
        {
            if (!db.Games.Any(existing => existing.Id == game.Id))
            {
                db.Games.Add(game);
            }
        }

        db.SaveChanges();
    }

    private static void EnsureTeam(
        AppDbContext db,
        int teamId,
        string name,
        string fullName,
        string abbreviation,
        string city,
        string conference,
        string division)
    {
        if (db.Teams.Any(t => t.Id == teamId))
            return;

        db.Teams.Add(new Team
        {
            Id = teamId,
            Name = name,
            FullName = fullName,
            Abbreviation = abbreviation,
            City = city,
            Conference = conference,
            Division = division,
        });
    }

    private static Game CreateGame(
        string id,
        int seasonId,
        int homeTeamId,
        int visitorTeamId,
        int homeScore,
        int visitorScore,
        DateTime date) => new()
        {
            Id = id,
            SeasonId = seasonId,
            Date = date,
            Status = "Final",
            HomeTeamId = homeTeamId,
            VisitorTeamId = visitorTeamId,
            HomeScore = homeScore,
            VisitorScore = visitorScore,
        };
}
