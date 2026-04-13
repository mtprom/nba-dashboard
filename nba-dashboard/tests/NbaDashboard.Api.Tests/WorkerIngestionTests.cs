using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NbaDashboard.Core.Entities;
using NbaDashboard.Infrastructure.Data;
using NbaDashboard.Infrastructure.NbaStats;
using NbaDashboard.Infrastructure.NbaStats.Models;
using NbaDashboard.Worker.Jobs;

namespace NbaDashboard.Api.Tests;

public class WorkerIngestionTests
{
    [Fact]
    public async Task SyncBoxScores_NullTraditional_DoesNotMarkGameDone()
    {
        using var db = CreateDbContext();
        var client = new ScriptedNbaStatsClient((endpoint, _) => endpoint switch
        {
            "leaguegamefinder" => CreateGameFinderResponse("0022300001", "2024-10-22"),
            "boxscoretraditionalv3" => null,
            _ => null,
        });

        var job = new SyncBoxScoresJob(client, db, NullLogger<SyncBoxScoresJob>.Instance);

        await job.RunAsync(new DateOnly(2024, 10, 22));

        Assert.False(await db.SyncStates.AnyAsync(s => s.Key == "boxscore_0022300001"));
        Assert.True(await db.SyncStates.AnyAsync(s => s.Key == "boxscore_retry_0022300001"));
        Assert.False(await db.Games.AnyAsync(g => g.Id == "0022300001"));
    }

    [Fact]
    public async Task SyncBoxScores_NullAdvanced_LeavesRetryableIncompleteCoverage()
    {
        using var db = CreateDbContext();
        var client = new ScriptedNbaStatsClient((endpoint, _) => endpoint switch
        {
            "leaguegamefinder" => CreateGameFinderResponse("0022300002", "2024-10-23"),
            "boxscoretraditionalv3" => CreateTraditionalResponse("0022300002"),
            "boxscoreadvancedv3" => null,
            _ => null,
        });

        var job = new SyncBoxScoresJob(client, db, NullLogger<SyncBoxScoresJob>.Instance);

        await job.RunAsync(new DateOnly(2024, 10, 23));

        Assert.True(await db.Games.AnyAsync(g => g.Id == "0022300002"));
        Assert.True(await db.PlayerGameStats.AnyAsync(s => s.GameId == "0022300002"));
        Assert.False(await db.PlayerGameAdvanced.AnyAsync(a => a.GameId == "0022300002"));
        Assert.False(await db.SyncStates.AnyAsync(s => s.Key == "boxscore_0022300002"));
        Assert.True(await db.SyncStates.AnyAsync(s => s.Key == "boxscore_retry_0022300002"));
    }

    [Fact]
    public async Task SyncBoxScores_FullResponses_MarkGameDoneAndPersistCoverage()
    {
        using var db = CreateDbContext();
        var client = new ScriptedNbaStatsClient((endpoint, _) => endpoint switch
        {
            "leaguegamefinder" => CreateGameFinderResponse("0022300003", "2024-10-24"),
            "boxscoretraditionalv3" => CreateTraditionalResponse("0022300003"),
            "boxscoreadvancedv3" => CreateAdvancedResponse("0022300003"),
            _ => null,
        });

        var job = new SyncBoxScoresJob(client, db, NullLogger<SyncBoxScoresJob>.Instance);

        await job.RunAsync(new DateOnly(2024, 10, 24));

        Assert.True(await db.Games.AnyAsync(g => g.Id == "0022300003"));
        Assert.Equal(2, await db.PlayerGameStats.CountAsync(s => s.GameId == "0022300003"));
        Assert.Equal(2, await db.PlayerGameAdvanced.CountAsync(a => a.GameId == "0022300003"));
        Assert.True(await db.SyncStates.AnyAsync(s => s.Key == "boxscore_0022300003"));
        Assert.False(await db.SyncStates.AnyAsync(s => s.Key == "boxscore_retry_0022300003"));
    }

    [Fact]
    public async Task HistoricalBackfill_DoesNotMarkSeasonCompleteWhenCoverageIsIncomplete()
    {
        using var db = CreateDbContext();
        var currentSeasonYear = DateTime.UtcNow.Month >= 10 ? DateTime.UtcNow.Year : DateTime.UtcNow.Year - 1;
        var priorSeasonYear = currentSeasonYear - 1;
        var priorSeasonLabel = $"{priorSeasonYear}-{(priorSeasonYear + 1) % 100:D2}";
        var currentSeasonLabel = $"{currentSeasonYear}-{(currentSeasonYear + 1) % 100:D2}";

        var client = new ScriptedNbaStatsClient((endpoint, parameters) =>
        {
            if (endpoint == "leaguegamefinder")
            {
                var season = parameters?["Season"];
                if (season == priorSeasonLabel)
                    return CreateGameFinderResponse("0022200999", $"{priorSeasonYear}-10-25");
                if (season == currentSeasonLabel)
                    return CreateEmptyGameFinderResponse();
            }

            return endpoint switch
            {
                "boxscoretraditionalv3" => CreateTraditionalResponse("0022200999"),
                "boxscoreadvancedv3" => null,
                _ => null,
            };
        });

        var syncJob = new SyncBoxScoresJob(client, db, NullLogger<SyncBoxScoresJob>.Instance);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["NbaStats:BackfillStartDate"] = $"{priorSeasonYear}-10-01",
            })
            .Build();
        var backfill = new HistoricalBackfillJob(syncJob, db, NullLogger<HistoricalBackfillJob>.Instance, config);

        await backfill.RunAsync();

        Assert.False(await db.SyncStates.AnyAsync(s => s.Key == $"backfill_season_{priorSeasonYear}"));
    }

    [Fact]
    public async Task SyncStandings_CreatesTeamsWhenDatabaseStartsEmpty()
    {
        using var db = CreateDbContext();
        var client = new ScriptedNbaStatsClient((endpoint, _) => endpoint switch
        {
            "leaguestandingsv3" => CreateStandingsResponse(),
            "leaguedashteamstats" => CreateTeamAdvancedStatsResponse(),
            _ => null,
        });

        var job = new SyncStandingsJob(
            client,
            db,
            NullLogger<SyncStandingsJob>.Instance,
            CreateStandingsConfig());

        await job.RunAsync();

        var team = await db.Teams.FindAsync(1610612738);
        Assert.NotNull(team);
        Assert.Equal("Boston", team!.City);
        Assert.Equal("Celtics", team.Name);
        var snapshot = await db.StandingsSnapshots.SingleAsync(s => s.TeamId == 1610612738);
        Assert.Equal(115.4m, snapshot.OffRating);
        Assert.Equal(108.9m, snapshot.DefRating);
        Assert.Equal(6.5m, snapshot.NetRating);
        Assert.Equal(99.1m, snapshot.Pace);
    }

    [Fact]
    public async Task SyncStandings_MissingAdvancedHeaders_DoesNotWriteSnapshotOrCursor()
    {
        using var db = CreateDbContext();
        var client = new ScriptedNbaStatsClient((endpoint, _) => endpoint switch
        {
            "leaguestandingsv3" => CreateStandingsResponse(),
            "leaguedashteamstats" => CreateTeamAdvancedStatsResponseMissingPace(),
            _ => null,
        });

        var job = new SyncStandingsJob(
            client,
            db,
            NullLogger<SyncStandingsJob>.Instance,
            CreateStandingsConfig());

        await job.RunAsync();

        Assert.False(await db.StandingsSnapshots.AnyAsync());
        Assert.False(await db.SyncStates.AnyAsync(s => s.Key.StartsWith("standings_")));
    }

    [Fact]
    public async Task SyncStandings_ReplaysStaleZeroSnapshotDespiteExistingCursor()
    {
        using var db = CreateDbContext();
        var currentSeasonYear = DateTime.UtcNow.Month >= 10 ? DateTime.UtcNow.Year : DateTime.UtcNow.Year - 1;
        var season = new Season { Id = 1, Year = currentSeasonYear };
        db.Seasons.Add(season);
        db.Teams.Add(new Team
        {
            Id = 1610612738,
            City = "Boston",
            Name = "Celtics",
            FullName = "Boston Celtics",
            Abbreviation = "BOS",
            Conference = "East",
            Division = "Atlantic",
        });

        var today = DateOnly.FromDateTime(DateTime.UtcNow)
            .ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        db.StandingsSnapshots.Add(new StandingsSnapshot
        {
            TeamId = 1610612738,
            SeasonId = season.Id,
            SnapshotDate = today,
            Wins = 57,
            Losses = 22,
            WinPct = 0.722m,
            ConfRank = 1,
            DivRank = 1,
            HomeRecord = "30-9",
            AwayRecord = "27-13",
            Last10 = "7-3",
            Streak = "W 1",
            OffRating = 0m,
            DefRating = 0m,
            NetRating = 0m,
            Pace = 0m,
        });
        db.SyncStates.Add(new SyncState
        {
            Key = $"standings_{currentSeasonYear}-{(currentSeasonYear + 1) % 100:D2}_{DateOnly.FromDateTime(DateTime.UtcNow):yyyy-MM-dd}",
            Value = "done",
            UpdatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var client = new ScriptedNbaStatsClient((endpoint, _) => endpoint switch
        {
            "leaguestandingsv3" => CreateStandingsResponse(),
            "leaguedashteamstats" => CreateTeamAdvancedStatsResponse(),
            _ => null,
        });

        var job = new SyncStandingsJob(
            client,
            db,
            NullLogger<SyncStandingsJob>.Instance,
            CreateStandingsConfig(currentSeasonYear));

        await job.RunAsync();

        var repaired = await db.StandingsSnapshots.SingleAsync(s => s.TeamId == 1610612738);
        Assert.Equal(115.4m, repaired.OffRating);
        Assert.Equal(108.9m, repaired.DefRating);
        Assert.Equal(6.5m, repaired.NetRating);
        Assert.Equal(99.1m, repaired.Pace);
    }

    [Fact]
    public async Task SyncSeasonAverages_DoesNotMarkCompletedSeasonDoneWhenRowsSkippedForMissingDependencies()
    {
        using var db = CreateDbContext();
        var currentSeasonYear = DateTime.UtcNow.Month >= 10 ? DateTime.UtcNow.Year : DateTime.UtcNow.Year - 1;
        var priorSeasonYear = currentSeasonYear - 1;

        var client = new ScriptedNbaStatsClient((endpoint, parameters) =>
        {
            if (endpoint != "leaguedashplayerstats")
                return null;

            var season = parameters?["Season"];
            var measureType = parameters?["MeasureType"];
            if (season != SeasonString(priorSeasonYear))
                return CreateEmptySeasonStatsResponse();

            return measureType switch
            {
                "Base" => CreateTraditionalSeasonStatsResponse(),
                "Advanced" => CreateAdvancedSeasonStatsResponse(),
                _ => null,
            };
        });

        var job = new SyncSeasonAveragesJob(
            client,
            db,
            NullLogger<SyncSeasonAveragesJob>.Instance,
            CreateStandingsConfig(priorSeasonYear));

        await job.RunAsync();

        Assert.False(await db.PlayerSeasonStats.AnyAsync());
        Assert.False(await db.SyncStates.AnyAsync(s => s.Key == $"season_avg_{priorSeasonYear}"));
    }

    [Fact]
    public async Task SyncSeasonAverages_ReplaysStaleCursorWhenCompletedSeasonHasNoRows()
    {
        using var db = CreateDbContext();
        var currentSeasonYear = DateTime.UtcNow.Month >= 10 ? DateTime.UtcNow.Year : DateTime.UtcNow.Year - 1;
        var priorSeasonYear = currentSeasonYear - 1;

        db.Teams.Add(new Team
        {
            Id = 1610612738,
            City = "Boston",
            Name = "Celtics",
            FullName = "Boston Celtics",
            Abbreviation = "BOS",
        });
        db.Players.Add(new Player
        {
            Id = 1628369,
            FirstName = "Jayson",
            LastName = "Tatum",
            TeamId = 1610612738,
            IsActive = true,
        });
        db.SyncStates.Add(new SyncState
        {
            Key = $"season_avg_{priorSeasonYear}",
            Value = "done",
            UpdatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var client = new ScriptedNbaStatsClient((endpoint, parameters) =>
        {
            if (endpoint != "leaguedashplayerstats")
                return null;

            var season = parameters?["Season"];
            var measureType = parameters?["MeasureType"];
            if (season != SeasonString(priorSeasonYear))
                return CreateEmptySeasonStatsResponse();

            return measureType switch
            {
                "Base" => CreateTraditionalSeasonStatsResponse(),
                "Advanced" => CreateAdvancedSeasonStatsResponse(),
                _ => null,
            };
        });

        var job = new SyncSeasonAveragesJob(
            client,
            db,
            NullLogger<SyncSeasonAveragesJob>.Instance,
            CreateStandingsConfig(priorSeasonYear));

        await job.RunAsync();

        var stat = await db.PlayerSeasonStats.SingleAsync(s => s.PlayerId == 1628369);
        Assert.Equal(55, stat.GamesPlayed);
        Assert.Equal(27.4m, stat.PtsAvg);
        Assert.True(await db.SyncStates.AnyAsync(s => s.Key == $"season_avg_{priorSeasonYear}"));
    }

    [Fact]
    public async Task SyncSeasonAverages_SucceedsAfterBoxScoreBackfillCreatesDependencies()
    {
        using var db = CreateDbContext();
        var currentSeasonYear = DateTime.UtcNow.Month >= 10 ? DateTime.UtcNow.Year : DateTime.UtcNow.Year - 1;
        var priorSeasonYear = currentSeasonYear - 1;
        var priorSeasonGameDate = $"{priorSeasonYear}-10-24";

        var client = new ScriptedNbaStatsClient((endpoint, parameters) =>
        {
            if (endpoint == "leaguegamefinder")
            {
                var season = parameters?["Season"];
                return season == SeasonString(priorSeasonYear)
                    ? CreateGameFinderResponse("0022201111", priorSeasonGameDate)
                    : CreateEmptyGameFinderResponse();
            }

            return endpoint switch
            {
                "boxscoretraditionalv3" => CreateTraditionalResponse("0022201111"),
                "boxscoreadvancedv3" => CreateAdvancedResponse("0022201111"),
                "leaguedashplayerstats" => parameters?["Season"] == SeasonString(priorSeasonYear)
                    ? parameters?["MeasureType"] switch
                    {
                        "Base" => CreateTraditionalSeasonStatsResponse(),
                        "Advanced" => CreateAdvancedSeasonStatsResponse(),
                        _ => CreateEmptySeasonStatsResponse(),
                    }
                    : CreateEmptySeasonStatsResponse(),
                _ => null,
            };
        });

        var boxScoreJob = new SyncBoxScoresJob(client, db, NullLogger<SyncBoxScoresJob>.Instance);
        await boxScoreJob.RunForSeasonAsync(priorSeasonYear);

        var seasonJob = new SyncSeasonAveragesJob(
            client,
            db,
            NullLogger<SyncSeasonAveragesJob>.Instance,
            CreateStandingsConfig(priorSeasonYear));

        await seasonJob.RunAsync();

        var stat = await db.PlayerSeasonStats.SingleAsync(s => s.PlayerId == 1628369);
        Assert.Equal(priorSeasonYear, stat.Season.Year);
        Assert.Equal(0.602m, stat.TsPct);
        Assert.True(await db.SyncStates.AnyAsync(s => s.Key == $"season_avg_{priorSeasonYear}"));
    }

    private static AppDbContext CreateDbContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static LeagueGameFinderResponse CreateGameFinderResponse(string gameId, string gameDate)
    {
        return new LeagueGameFinderResponse
        {
            ResultSets =
            [
                new GameFinderResultSet
                {
                    Name = "LeagueGameFinderResults",
                    Headers = ["GAME_ID", "GAME_DATE"],
                    RowSet =
                    [
                        [ToJson(gameId), ToJson(gameDate)],
                        [ToJson(gameId), ToJson(gameDate)],
                    ],
                },
            ],
        };
    }

    private static LeagueGameFinderResponse CreateEmptyGameFinderResponse()
    {
        return new LeagueGameFinderResponse
        {
            ResultSets =
            [
                new GameFinderResultSet
                {
                    Name = "LeagueGameFinderResults",
                    Headers = ["GAME_ID", "GAME_DATE"],
                    RowSet = [],
                },
            ],
        };
    }

    private static BoxScoreTraditionalV3Response CreateTraditionalResponse(string gameId)
    {
        return new BoxScoreTraditionalV3Response
        {
            BoxScoreTraditional = new BoxScoreTraditional
            {
                GameId = gameId,
                HomeTeamId = 1610612738,
                AwayTeamId = 1610612747,
                HomeTeam = new TraditionalTeam
                {
                    TeamId = 1610612738,
                    TeamCity = "Boston",
                    TeamName = "Celtics",
                    TeamTricode = "BOS",
                    Players =
                    [
                        new TraditionalPlayer
                        {
                            PersonId = 1628369,
                            FirstName = "Jayson",
                            FamilyName = "Tatum",
                            Position = "F",
                            JerseyNum = "0",
                            Statistics = new TraditionalStatistics
                            {
                                Minutes = "35:00",
                                FieldGoalsMade = 10,
                                FieldGoalsAttempted = 20,
                                FieldGoalsPercentage = 0.5,
                                ThreePointersMade = 3,
                                ThreePointersAttempted = 8,
                                ThreePointersPercentage = 0.375,
                                FreeThrowsMade = 5,
                                FreeThrowsAttempted = 6,
                                FreeThrowsPercentage = 0.833,
                                ReboundsOffensive = 1,
                                ReboundsDefensive = 7,
                                ReboundsTotal = 8,
                                Assists = 5,
                                Steals = 1,
                                Blocks = 1,
                                Turnovers = 2,
                                FoulsPersonal = 2,
                                Points = 28,
                                PlusMinusPoints = 7,
                            },
                        },
                    ],
                },
                AwayTeam = new TraditionalTeam
                {
                    TeamId = 1610612747,
                    TeamCity = "Los Angeles",
                    TeamName = "Lakers",
                    TeamTricode = "LAL",
                    Players =
                    [
                        new TraditionalPlayer
                        {
                            PersonId = 2544,
                            FirstName = "LeBron",
                            FamilyName = "James",
                            Position = "F",
                            JerseyNum = "23",
                            Statistics = new TraditionalStatistics
                            {
                                Minutes = "34:00",
                                FieldGoalsMade = 9,
                                FieldGoalsAttempted = 18,
                                FieldGoalsPercentage = 0.5,
                                ThreePointersMade = 2,
                                ThreePointersAttempted = 6,
                                ThreePointersPercentage = 0.333,
                                FreeThrowsMade = 4,
                                FreeThrowsAttempted = 5,
                                FreeThrowsPercentage = 0.8,
                                ReboundsOffensive = 1,
                                ReboundsDefensive = 8,
                                ReboundsTotal = 9,
                                Assists = 7,
                                Steals = 1,
                                Blocks = 0,
                                Turnovers = 3,
                                FoulsPersonal = 1,
                                Points = 24,
                                PlusMinusPoints = -7,
                            },
                        },
                    ],
                },
            },
        };
    }

    private static BoxScoreAdvancedV3Response CreateAdvancedResponse(string gameId)
    {
        return new BoxScoreAdvancedV3Response
        {
            BoxScoreAdvanced = new BoxScoreAdvanced
            {
                GameId = gameId,
                HomeTeamId = 1610612738,
                AwayTeamId = 1610612747,
                HomeTeam = new AdvancedTeam
                {
                    TeamId = 1610612738,
                    Players =
                    [
                        new AdvancedPlayer
                        {
                            PersonId = 1628369,
                            Statistics = new AdvancedStatistics
                            {
                                Minutes = "35:00",
                                OffensiveRating = 118,
                                DefensiveRating = 104,
                                NetRating = 14,
                                AssistPercentage = 21,
                                OffensiveReboundPercentage = 3,
                                DefensiveReboundPercentage = 20,
                                ReboundPercentage = 12,
                                EffectiveFieldGoalPercentage = 0.575,
                                TrueShootingPercentage = 0.612,
                                UsagePercentage = 31,
                                Pace = 99,
                                PIE = 0.19,
                            },
                        },
                    ],
                },
                AwayTeam = new AdvancedTeam
                {
                    TeamId = 1610612747,
                    Players =
                    [
                        new AdvancedPlayer
                        {
                            PersonId = 2544,
                            Statistics = new AdvancedStatistics
                            {
                                Minutes = "34:00",
                                OffensiveRating = 109,
                                DefensiveRating = 118,
                                NetRating = -9,
                                AssistPercentage = 34,
                                OffensiveReboundPercentage = 2,
                                DefensiveReboundPercentage = 24,
                                ReboundPercentage = 13,
                                EffectiveFieldGoalPercentage = 0.556,
                                TrueShootingPercentage = 0.601,
                                UsagePercentage = 29,
                                Pace = 99,
                                PIE = 0.17,
                            },
                        },
                    ],
                },
            },
        };
    }

    private static LeagueStandingsV3Response CreateStandingsResponse()
    {
        return new LeagueStandingsV3Response
        {
            ResultSets =
            [
                new StandingsResultSet
                {
                    Name = "Standings",
                    Headers =
                    [
                        "TeamID", "TeamCity", "TeamName", "TeamAbbreviation", "Conference", "Division",
                        "WINS", "LOSSES", "WinPCT", "PlayoffRank", "DivisionRank", "HOME", "ROAD", "L10",
                        "strCurrentStreak"
                    ],
                    RowSet =
                    [
                        [
                            ToJson(1610612738), ToJson("Boston"), ToJson("Celtics"), ToJson("BOS"),
                            ToJson("East"), ToJson("Atlantic"), ToJson(57), ToJson(22), ToJson(0.722m),
                            ToJson(1), ToJson(1), ToJson("30-9"), ToJson("27-13"), ToJson("7-3"),
                            ToJson("W 1")
                        ],
                    ],
                },
            ],
        };
    }

    private static LeagueDashTeamStatsResponse CreateTeamAdvancedStatsResponse()
    {
        return new LeagueDashTeamStatsResponse
        {
            ResultSets =
            [
                new TeamStatsResultSet
                {
                    Name = "LeagueDashTeamStats",
                    Headers =
                    [
                        "TEAM_ID", "TEAM_NAME", "E_OFF_RATING", "OFF_RATING", "E_DEF_RATING",
                        "DEF_RATING", "E_NET_RATING", "NET_RATING", "E_PACE", "PACE"
                    ],
                    RowSet =
                    [
                        [
                            ToJson(1610612738), ToJson("Boston Celtics"), ToJson(114.8m), ToJson(115.4m),
                            ToJson(109.1m), ToJson(108.9m), ToJson(5.7m), ToJson(6.5m), ToJson(99.8m),
                            ToJson(99.1m)
                        ],
                    ],
                },
            ],
        };
    }

    private static LeagueDashTeamStatsResponse CreateTeamAdvancedStatsResponseMissingPace()
    {
        return new LeagueDashTeamStatsResponse
        {
            ResultSets =
            [
                new TeamStatsResultSet
                {
                    Name = "LeagueDashTeamStats",
                    Headers =
                    [
                        "TEAM_ID", "TEAM_NAME", "OFF_RATING", "DEF_RATING", "NET_RATING"
                    ],
                    RowSet =
                    [
                        [
                            ToJson(1610612738), ToJson("Boston Celtics"), ToJson(115.4m), ToJson(108.9m),
                            ToJson(6.5m)
                        ],
                    ],
                },
            ],
        };
    }

    private static LeagueDashPlayerStatsResponse CreateTraditionalSeasonStatsResponse()
    {
        return new LeagueDashPlayerStatsResponse
        {
            ResultSets =
            [
                new PlayerStatsResultSet
                {
                    Name = "LeagueDashPlayerStats",
                    Headers =
                    [
                        "PLAYER_ID", "TEAM_ID", "GP", "MIN", "PTS", "REB", "AST", "STL",
                        "BLK", "TOV", "FG_PCT", "FG3_PCT", "FT_PCT"
                    ],
                    RowSet =
                    [
                        [
                            ToJson(1628369), ToJson(1610612738), ToJson(55), ToJson(36.1m), ToJson(27.4m),
                            ToJson(8.2m), ToJson(5.8m), ToJson(1.1m), ToJson(0.7m), ToJson(2.9m),
                            ToJson(0.482m), ToJson(0.381m), ToJson(0.854m)
                        ],
                    ],
                },
            ],
        };
    }

    private static LeagueDashPlayerStatsResponse CreateAdvancedSeasonStatsResponse()
    {
        return new LeagueDashPlayerStatsResponse
        {
            ResultSets =
            [
                new PlayerStatsResultSet
                {
                    Name = "LeagueDashPlayerStats",
                    Headers =
                    [
                        "PLAYER_ID", "TEAM_ID", "TS_PCT", "USG_PCT", "NET_RATING", "PIE"
                    ],
                    RowSet =
                    [
                        [
                            ToJson(1628369), ToJson(1610612738), ToJson(0.602m), ToJson(30.4m),
                            ToJson(8.6m), ToJson(0.174m)
                        ],
                    ],
                },
            ],
        };
    }

    private static LeagueDashPlayerStatsResponse CreateEmptySeasonStatsResponse()
    {
        return new LeagueDashPlayerStatsResponse
        {
            ResultSets =
            [
                new PlayerStatsResultSet
                {
                    Name = "LeagueDashPlayerStats",
                    Headers = [],
                    RowSet = [],
                },
            ],
        };
    }

    private static IConfiguration CreateStandingsConfig(int? backfillStartYear = null)
    {
        var effectiveStartYear = backfillStartYear
            ?? (DateTime.UtcNow.Month >= 10 ? DateTime.UtcNow.Year : DateTime.UtcNow.Year - 1);

        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["NbaStats:BackfillStartDate"] = $"{effectiveStartYear}-10-01",
            })
            .Build();
    }

    private static string SeasonString(int year) =>
        $"{year}-{(year + 1) % 100:D2}";

    private static JsonElement ToJson<T>(T value) => JsonSerializer.SerializeToElement(value);

    private sealed class ScriptedNbaStatsClient : NbaStatsClient
    {
        private readonly Func<string, Dictionary<string, string>?, object?> _handler;

        public ScriptedNbaStatsClient(Func<string, Dictionary<string, string>?, object?> handler)
            : base(new HttpClient(), NullLogger<NbaStatsClient>.Instance)
        {
            _handler = handler;
        }

        public override Task<T?> GetAsync<T>(string endpoint, Dictionary<string, string>? parameters = null,
            CancellationToken ct = default) where T : default
        {
            return Task.FromResult((T?)_handler(endpoint, parameters));
        }
    }
}
