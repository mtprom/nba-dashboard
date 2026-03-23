using NbaDashboard.Core.Entities;
using NbaDashboard.Infrastructure.Data;

namespace NbaDashboard.Api.Tests.Helpers;

public static class TestDataSeeder
{
    public const int CelticsId = 1610612738;
    public const int LakersId = 1610612747;
    public const int ThunderId = 1610612760;

    public static void Seed(AppDbContext db)
    {
        var season = new Season { Id = 1, Year = 2025 };
        db.Seasons.Add(season);

        var celtics = new Team
        {
            Id = CelticsId, Name = "Celtics", FullName = "Boston Celtics",
            Abbreviation = "BOS", City = "Boston", Conference = "East", Division = "Atlantic"
        };
        var lakers = new Team
        {
            Id = LakersId, Name = "Lakers", FullName = "Los Angeles Lakers",
            Abbreviation = "LAL", City = "Los Angeles", Conference = "West", Division = "Pacific"
        };
        var thunder = new Team
        {
            Id = ThunderId, Name = "Thunder", FullName = "Oklahoma City Thunder",
            Abbreviation = "OKC", City = "Oklahoma City", Conference = "West", Division = "Northwest"
        };
        db.Teams.AddRange(celtics, lakers, thunder);

        var tatum = new Player
        {
            Id = 1628369, FirstName = "Jayson", LastName = "Tatum",
            Position = "SF", JerseyNumber = "0", TeamId = CelticsId, IsActive = true
        };
        var brown = new Player
        {
            Id = 1627759, FirstName = "Jaylen", LastName = "Brown",
            Position = "SG", JerseyNumber = "7", TeamId = CelticsId, IsActive = true
        };
        var james = new Player
        {
            Id = 2544, FirstName = "LeBron", LastName = "James",
            Position = "SF", JerseyNumber = "23", TeamId = LakersId, IsActive = true
        };
        var davis = new Player
        {
            Id = 203076, FirstName = "Anthony", LastName = "Davis",
            Position = "PF", JerseyNumber = "3", TeamId = LakersId, IsActive = true
        };
        db.Players.AddRange(tatum, brown, james, davis);

        // 5 games: Celtics win 3, Lakers win 2
        var gameData = new[]
        {
            ("0022500101", CelticsId, LakersId, 117, 108, new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc)),
            ("0022500202", LakersId, CelticsId, 112, 105, new DateTime(2026, 1, 22, 0, 0, 0, DateTimeKind.Utc)),
            ("0022500303", CelticsId, LakersId, 121, 115, new DateTime(2026, 2, 5, 0, 0, 0, DateTimeKind.Utc)),
            ("0022500404", LakersId, CelticsId, 110, 120, new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc)),
            ("0022500505", CelticsId, LakersId, 99, 104, new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)),
        };

        // Game results: Celtics win games 1,3,4 (home wins + away win), Lakers win games 2,5
        foreach (var (id, homeId, visitorId, homeScore, visitorScore, date) in gameData)
        {
            var game = new Game
            {
                Id = id, SeasonId = 1, Date = date, Status = "Final",
                HomeTeamId = homeId, VisitorTeamId = visitorId,
                HomeScore = homeScore, VisitorScore = visitorScore,
                Arena = homeId == CelticsId ? "TD Garden" : "Crypto.com Arena"
            };
            db.Games.Add(game);

            // Add player stats for each game
            AddPlayerStats(db, id, tatum, CelticsId, 28, 8, 5, 1, 0, 2, 38m, 0.480m, 0.400m, 0.900m, 10);
            AddPlayerStats(db, id, brown, CelticsId, 22, 5, 3, 2, 1, 1, 34m, 0.450m, 0.350m, 0.850m, 8);
            AddPlayerStats(db, id, james, LakersId, 25, 7, 8, 1, 1, 3, 36m, 0.500m, 0.333m, 0.750m, 5);
            AddPlayerStats(db, id, davis, LakersId, 24, 11, 3, 1, 2, 2, 35m, 0.520m, 0.000m, 0.800m, 7);
        }

        // Season stats
        db.PlayerSeasonStats.AddRange(
            new PlayerSeasonStats
            {
                PlayerId = tatum.Id, SeasonId = 1, TeamId = CelticsId,
                GamesPlayed = 58, PtsAvg = 27.1m, RebAvg = 8.4m, AstAvg = 4.9m,
                StlAvg = 1.1m, BlkAvg = 0.6m, ToAvg = 2.8m,
                FgPct = 0.464m, Fg3Pct = 0.378m, FtPct = 0.857m, TsPct = 0.594m,
                UsgPct = 30.2m, NetRating = 8.5m, Pie = 0.168m, Per = 25.3m
            },
            new PlayerSeasonStats
            {
                PlayerId = brown.Id, SeasonId = 1, TeamId = CelticsId,
                GamesPlayed = 55, PtsAvg = 23.5m, RebAvg = 5.8m, AstAvg = 3.2m,
                StlAvg = 1.2m, BlkAvg = 0.5m, ToAvg = 2.3m,
                FgPct = 0.478m, Fg3Pct = 0.362m, FtPct = 0.786m, TsPct = 0.580m,
                UsgPct = 27.1m, NetRating = 6.8m, Pie = 0.145m, Per = 21.7m
            },
            new PlayerSeasonStats
            {
                PlayerId = james.Id, SeasonId = 1, TeamId = LakersId,
                GamesPlayed = 52, PtsAvg = 25.4m, RebAvg = 7.1m, AstAvg = 8.2m,
                StlAvg = 1.3m, BlkAvg = 0.5m, ToAvg = 3.5m,
                FgPct = 0.510m, Fg3Pct = 0.395m, FtPct = 0.730m, TsPct = 0.620m,
                UsgPct = 29.5m, NetRating = 4.2m, Pie = 0.155m, Per = 24.1m
            },
            new PlayerSeasonStats
            {
                PlayerId = davis.Id, SeasonId = 1, TeamId = LakersId,
                GamesPlayed = 50, PtsAvg = 24.8m, RebAvg = 12.3m, AstAvg = 3.5m,
                StlAvg = 1.4m, BlkAvg = 2.1m, ToAvg = 2.1m,
                FgPct = 0.540m, Fg3Pct = 0.267m, FtPct = 0.815m, TsPct = 0.610m,
                UsgPct = 28.0m, NetRating = 5.1m, Pie = 0.162m, Per = 27.5m
            }
        );

        db.SaveChanges();
    }

    private static void AddPlayerStats(AppDbContext db, string gameId, Player player, int teamId,
        int pts, int reb, int ast, int stl, int blk, int to, decimal min,
        decimal fgPct, decimal threePct, decimal ftPct, int plusMinus)
    {
        db.PlayerGameStats.Add(new PlayerGameStats
        {
            GameId = gameId, PlayerId = player.Id, TeamId = teamId,
            Points = pts, Rebounds = reb, Assists = ast,
            Steals = stl, Blocks = blk, Turnovers = to,
            Minutes = min, FieldGoalPct = fgPct, ThreePointPct = threePct,
            FreeThrowPct = ftPct, PlusMinus = plusMinus,
        });
    }
}
