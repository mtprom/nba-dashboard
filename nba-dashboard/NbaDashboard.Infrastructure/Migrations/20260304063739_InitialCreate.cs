using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NbaDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Seasons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seasons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncStates",
                columns: table => new
                {
                    Key = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncStates", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Abbreviation = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Conference = table.Column<string>(type: "text", nullable: false),
                    Division = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    SeasonId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Period = table.Column<int>(type: "integer", nullable: false),
                    TimeRemaining = table.Column<string>(type: "text", nullable: false),
                    HomeTeamId = table.Column<int>(type: "integer", nullable: false),
                    VisitorTeamId = table.Column<int>(type: "integer", nullable: false),
                    HomeScore = table.Column<int>(type: "integer", nullable: false),
                    VisitorScore = table.Column<int>(type: "integer", nullable: false),
                    Postseason = table.Column<bool>(type: "boolean", nullable: false),
                    Arena = table.Column<string>(type: "text", nullable: false),
                    IngestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Games_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Games_Teams_HomeTeamId",
                        column: x => x.HomeTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Games_Teams_VisitorTeamId",
                        column: x => x.VisitorTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Position = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Height = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Weight = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    JerseyNumber = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Players_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StandingsSnapshots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    SeasonId = table.Column<int>(type: "integer", nullable: false),
                    SnapshotDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Wins = table.Column<int>(type: "integer", nullable: false),
                    Losses = table.Column<int>(type: "integer", nullable: false),
                    WinPct = table.Column<decimal>(type: "numeric", nullable: false),
                    ConfRank = table.Column<int>(type: "integer", nullable: false),
                    DivRank = table.Column<int>(type: "integer", nullable: false),
                    HomeRecord = table.Column<string>(type: "text", nullable: false),
                    AwayRecord = table.Column<string>(type: "text", nullable: false),
                    Last10 = table.Column<string>(type: "text", nullable: false),
                    Streak = table.Column<string>(type: "text", nullable: false),
                    OffRating = table.Column<decimal>(type: "numeric", nullable: false),
                    DefRating = table.Column<decimal>(type: "numeric", nullable: false),
                    NetRating = table.Column<decimal>(type: "numeric", nullable: false),
                    Pace = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StandingsSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StandingsSnapshots_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StandingsSnapshots_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayerGameAdvanced",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GameId = table.Column<string>(type: "text", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    Minutes = table.Column<decimal>(type: "numeric", nullable: false),
                    OffRating = table.Column<decimal>(type: "numeric", nullable: false),
                    DefRating = table.Column<decimal>(type: "numeric", nullable: false),
                    NetRating = table.Column<decimal>(type: "numeric", nullable: false),
                    AstPct = table.Column<decimal>(type: "numeric", nullable: false),
                    OrebPct = table.Column<decimal>(type: "numeric", nullable: false),
                    DrebPct = table.Column<decimal>(type: "numeric", nullable: false),
                    RebPct = table.Column<decimal>(type: "numeric", nullable: false),
                    EfgPct = table.Column<decimal>(type: "numeric", nullable: false),
                    TsPct = table.Column<decimal>(type: "numeric", nullable: false),
                    UsgPct = table.Column<decimal>(type: "numeric", nullable: false),
                    Pace = table.Column<decimal>(type: "numeric", nullable: false),
                    Pie = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerGameAdvanced", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerGameAdvanced_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerGameAdvanced_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerGameAdvanced_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayerGameStats",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GameId = table.Column<string>(type: "text", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    StartPosition = table.Column<string>(type: "text", nullable: false),
                    Minutes = table.Column<decimal>(type: "numeric", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    Rebounds = table.Column<int>(type: "integer", nullable: false),
                    Assists = table.Column<int>(type: "integer", nullable: false),
                    Steals = table.Column<int>(type: "integer", nullable: false),
                    Blocks = table.Column<int>(type: "integer", nullable: false),
                    Turnovers = table.Column<int>(type: "integer", nullable: false),
                    PersonalFouls = table.Column<int>(type: "integer", nullable: false),
                    PlusMinus = table.Column<int>(type: "integer", nullable: false),
                    FieldGoalsMade = table.Column<int>(type: "integer", nullable: false),
                    FieldGoalsAttempted = table.Column<int>(type: "integer", nullable: false),
                    FieldGoalPct = table.Column<decimal>(type: "numeric", nullable: false),
                    ThreePointersMade = table.Column<int>(type: "integer", nullable: false),
                    ThreePointersAttempted = table.Column<int>(type: "integer", nullable: false),
                    ThreePointPct = table.Column<decimal>(type: "numeric", nullable: false),
                    FreeThrowsMade = table.Column<int>(type: "integer", nullable: false),
                    FreeThrowsAttempted = table.Column<int>(type: "integer", nullable: false),
                    FreeThrowPct = table.Column<decimal>(type: "numeric", nullable: false),
                    OffensiveRebounds = table.Column<int>(type: "integer", nullable: false),
                    DefensiveRebounds = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerGameStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerGameStats_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerGameStats_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerGameStats_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayerHeat",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    ComputedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HeatScore = table.Column<decimal>(type: "numeric", nullable: false),
                    GamesSampled = table.Column<int>(type: "integer", nullable: false),
                    PtsAvg = table.Column<decimal>(type: "numeric", nullable: false),
                    TsPctAvg = table.Column<decimal>(type: "numeric", nullable: false),
                    UsgPctAvg = table.Column<decimal>(type: "numeric", nullable: false),
                    NetRatingAvg = table.Column<decimal>(type: "numeric", nullable: false),
                    PieAvg = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerHeat", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerHeat_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerSeasonStats",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    SeasonId = table.Column<int>(type: "integer", nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    GamesPlayed = table.Column<int>(type: "integer", nullable: false),
                    MinAvg = table.Column<decimal>(type: "numeric", nullable: false),
                    PtsAvg = table.Column<decimal>(type: "numeric", nullable: false),
                    RebAvg = table.Column<decimal>(type: "numeric", nullable: false),
                    AstAvg = table.Column<decimal>(type: "numeric", nullable: false),
                    StlAvg = table.Column<decimal>(type: "numeric", nullable: false),
                    BlkAvg = table.Column<decimal>(type: "numeric", nullable: false),
                    ToAvg = table.Column<decimal>(type: "numeric", nullable: false),
                    FgPct = table.Column<decimal>(type: "numeric", nullable: false),
                    Fg3Pct = table.Column<decimal>(type: "numeric", nullable: false),
                    FtPct = table.Column<decimal>(type: "numeric", nullable: false),
                    TsPct = table.Column<decimal>(type: "numeric", nullable: false),
                    UsgPct = table.Column<decimal>(type: "numeric", nullable: false),
                    NetRating = table.Column<decimal>(type: "numeric", nullable: false),
                    Pie = table.Column<decimal>(type: "numeric", nullable: false),
                    Per = table.Column<decimal>(type: "numeric", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerSeasonStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerSeasonStats_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerSeasonStats_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerSeasonStats_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Games_Date",
                table: "Games",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Games_HomeTeamId",
                table: "Games",
                column: "HomeTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_SeasonId",
                table: "Games",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_VisitorTeamId",
                table: "Games",
                column: "VisitorTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerGameAdvanced_GameId_PlayerId",
                table: "PlayerGameAdvanced",
                columns: new[] { "GameId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerGameAdvanced_PlayerId",
                table: "PlayerGameAdvanced",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerGameAdvanced_TeamId",
                table: "PlayerGameAdvanced",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerGameStats_GameId_PlayerId",
                table: "PlayerGameStats",
                columns: new[] { "GameId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerGameStats_PlayerId",
                table: "PlayerGameStats",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerGameStats_TeamId",
                table: "PlayerGameStats",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerHeat_ComputedDate_PlayerId",
                table: "PlayerHeat",
                columns: new[] { "ComputedDate", "PlayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerHeat_PlayerId_ComputedDate",
                table: "PlayerHeat",
                columns: new[] { "PlayerId", "ComputedDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_TeamId",
                table: "Players",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSeasonStats_PlayerId_SeasonId_TeamId",
                table: "PlayerSeasonStats",
                columns: new[] { "PlayerId", "SeasonId", "TeamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSeasonStats_SeasonId",
                table: "PlayerSeasonStats",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSeasonStats_TeamId",
                table: "PlayerSeasonStats",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Seasons_Year",
                table: "Seasons",
                column: "Year",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StandingsSnapshots_SeasonId",
                table: "StandingsSnapshots",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_StandingsSnapshots_TeamId_SeasonId_SnapshotDate",
                table: "StandingsSnapshots",
                columns: new[] { "TeamId", "SeasonId", "SnapshotDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerGameAdvanced");

            migrationBuilder.DropTable(
                name: "PlayerGameStats");

            migrationBuilder.DropTable(
                name: "PlayerHeat");

            migrationBuilder.DropTable(
                name: "PlayerSeasonStats");

            migrationBuilder.DropTable(
                name: "StandingsSnapshots");

            migrationBuilder.DropTable(
                name: "SyncStates");

            migrationBuilder.DropTable(
                name: "Games");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Seasons");

            migrationBuilder.DropTable(
                name: "Teams");
        }
    }
}
