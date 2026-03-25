# NBA Dashboard — Codebase Analysis

> Generated 2026-03-24. Reference for future Claude instances working on this project.

## Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| API | ASP.NET Core 8 | .NET 8 |
| Worker | .NET Worker Service + Hangfire | .NET 8 |
| Frontend | React + TypeScript + Vite + Tailwind CSS | React 18.3, TS 5.4, Vite 5.2, Tailwind 3.4 |
| Database | PostgreSQL 16 | EF Core 8 |
| Charts | Recharts | 2.12 |
| Routing | react-router-dom | 6.23 |
| HTTP (frontend) | axios | 1.7 |
| UI Components | Radix UI + custom | lucide-react for icons |

**Working directory:** `/Users/macprom/Desktop/nba/nba-dashboard`
**dotnet path:** `/usr/local/share/dotnet/dotnet`

---

## Deployment

**The project runs Docker-containerized on an SSH server.** The local machine is just used for editing code — preview tools (browser screenshots, etc.) do NOT work. Verification must be done via `curl` against the API or `docker` commands over SSH.

## Docker Compose

All services defined in `docker-compose.yml`. **Containers run under these names/ports:**

| Service | Container Name | Exposed Port | Internal Port |
|---------|---------------|-------------|---------------|
| db | nba-dashboard-db-1 | 5432 | 5432 |
| api | nba-dashboard-api-1 | **5000** | 8080 |
| worker | nba-dashboard-worker-1 | none | — |
| frontend | nba-dashboard-frontend-1 | 5173 | 5173 |

**DB Credentials (from container env):**
- `POSTGRES_USER=nba_user`
- `POSTGRES_PASSWORD=hooker`
- `POSTGRES_DB=nba_dashboard`

**⚠️ Important:** The API runs on port **5000** (not 5062). Always use `http://localhost:5000/api/...` for curl testing.

**DB queries from host:**
```bash
docker exec nba-dashboard-db-1 psql -U nba_user -d nba_dashboard -c '<SQL>'
```

**EF Core uses PascalCase table/column names — always quote them:**
```sql
SELECT "Id", "Abbreviation", "Name" FROM "Teams" LIMIT 10;
```

---

## Project Structure

```
nba-dashboard/
├── NbaDashboard.sln
├── docker-compose.yml
├── .env.example
├── NbaDashboard.Core/              # Domain entities (no dependencies)
│   └── Entities/
├── NbaDashboard.Infrastructure/    # EF Core DbContext, NBA API client, models
│   ├── Data/AppDbContext.cs
│   ├── NbaStats/NbaStatsClient.cs
│   ├── NbaStats/NbaStatsHeaders.cs
│   └── NbaStats/Models/
├── NbaDashboard.Api/               # ASP.NET Core API
│   ├── Controllers/
│   ├── DTOs/
│   └── Program.cs
├── NbaDashboard.Worker/            # Hangfire worker service
│   ├── Jobs/
│   └── Program.cs
├── nba-frontend/                   # React/Vite frontend
│   └── src/
│       ├── api/                    # axios API clients
│       ├── components/             # UI components
│       ├── data/teams.ts           # Hardcoded team colors, abbreviations, info
│       ├── hooks/
│       ├── pages/
│       └── types/index.ts
├── api-test/                       # Standalone NBA API test project
└── tests/NbaDashboard.Api.Tests/   # Integration tests (xUnit)
```

---

## Database Schema

**Tables (PascalCase, quoted in SQL):**

| Table | PK | Key Columns | Notes |
|-------|----|------------|-------|
| **Teams** | Id (int, manual) | Name, Abbreviation, City, FullName, Conference, Division | `ValueGeneratedNever` — NBA API team IDs (e.g. 1610612747) |
| **Players** | Id (int, manual) | FirstName, LastName, Position, Height, Weight, JerseyNumber, TeamId (FK), IsActive | |
| **Seasons** | Id (auto) | Year (unique) | e.g. Year=2025 for 2025-26 season |
| **Games** | Id (string) | SeasonId (FK), Date, Status, HomeTeamId (FK), VisitorTeamId (FK), HomeScore, VisitorScore, Arena, Postseason | Indexed on Date, SeasonId |
| **PlayerGameStats** | Id (auto) | GameId, PlayerId, TeamId, Minutes (decimal), Points, Rebounds, Assists, Steals, Blocks, Turnovers, FG/3P/FT counts & pcts, PlusMinus | Unique on (GameId, PlayerId) |
| **PlayerGameAdvanced** | Id (auto) | GameId, PlayerId, TeamId, OffRating, DefRating, NetRating, AstPct, RebPct, EfgPct, TsPct, UsgPct, Pace, Pie | Unique on (GameId, PlayerId) |
| **PlayerSeasonStats** | Id (auto) | PlayerId, SeasonId, TeamId, GamesPlayed, PtsAvg, RebAvg, AstAvg, StlAvg, BlkAvg, ToAvg, FgPct, Fg3Pct, FtPct, TsPct, UsgPct, NetRating, Pie, Per | Unique on (PlayerId, SeasonId, TeamId) |
| **PlayerHeat** | Id (auto) | PlayerId, ComputedDate, HeatScore, GamesSampled, PtsAvg, TsPctAvg, UsgPctAvg, NetRatingAvg, PieAvg | Unique on (PlayerId, ComputedDate) |
| **StandingsSnapshots** | Id (auto) | TeamId, SeasonId, SnapshotDate, Wins, Losses, WinPct, ConfRank, DivRank, HomeRecord, AwayRecord, Last10, Streak, OffRating, DefRating, NetRating, Pace | Unique on (TeamId, SeasonId, SnapshotDate) |
| **SyncStates** | Key (string PK) | Value, UpdatedAt | Idempotency tracking for worker jobs |

**Migrations:**
1. `InitialCreate` — Full schema
2. `DisableAutoIncrementForTeamAndPlayer` — Team & Player IDs are manually assigned from NBA API

---

## API Endpoints

**Base:** `http://localhost:5000/api`

| Method | Route | Returns | Cache | Notes |
|--------|-------|---------|-------|-------|
| GET | `/games/upcoming` | `UpcomingGameDto[]` | 30 min | Today's games from scoreboardv2; falls back to LineScore when teams missing from DB |
| GET | `/teams/{teamId}/matchup/{opponentId}` | `MatchupHistoryDto` | — | Last 5 games between two teams with player stats |
| GET | `/players/season-averages?teamIds=1,2` | `Dict<int, PlayerSeasonAvgDto>` | — | Season averages for players on given teams |
| GET | `/standings?season=2025` | `StandingsDto[]` | — | Latest standings snapshot (defaults to current season) |
| GET | `/hot/players?window=5\|10\|season` | `HotPlayerDto[]` | 5 min | Hot/cold players with delta stats |
| GET | `/hot/teams?window=5\|10\|season` | `HotTeamDto[]` | 5 min | Hot/cold teams with delta stats |

### Key DTOs

**UpcomingGameDto:**
```json
{
  "game": { "id": "0022501038", "date": "...", "status": "Final|Scheduled|In Progress", "homeTeamId": 1610612765, "visitorTeamId": 1610612747, "homeScore": 113, "visitorScore": 110, "arena": "..." },
  "homeTeam": { "id": 1610612765, "name": "Pistons", "fullName": "Detroit Pistons", "abbreviation": "DET", "city": "Detroit", "conference": "East", "division": "Central" },
  "visitorTeam": { ... }
}
```

**HotPlayerDto heat score formula:** Points delta (35%) + TS% delta (25%) + Assists delta (15%) + Net rating delta (15%) + Rebounds delta (10%)

---

## NBA API Client (NbaStatsClient)

**File:** `NbaDashboard.Infrastructure/NbaStats/NbaStatsClient.cs`

- **Base URL:** `https://stats.nba.com/stats` (hardcoded, not DI)
- **Throttling:** 6,000–7,000 ms random delay between requests (SemaphoreSlim)
- **Method:** Spawns `curl` process with Chrome 131 headers
- **Decompression:** gzip/deflate/brotli
- **Timeout:** 30s per request
- **Headers:** Defined in `NbaStatsHeaders.cs` — Chrome 131 user agent, referer, etc.
- **JSON:** `PropertyNameCaseInsensitive = true` (handles PIE vs pie, etc.)

---

## NBA API Response Formats

| Endpoint | Format | Structure |
|----------|--------|-----------|
| `leaguegamefinder` | v2 envelope | `resultSets[].headers[] + resultSets[].rowSet[][]` (positional arrays) |
| `boxscoretraditionalv3` | v3 nested | `boxScoreTraditional.homeTeam/awayTeam.players[].statistics` |
| `boxscoreadvancedv3` | v3 nested | `boxScoreAdvanced.homeTeam/awayTeam.players[].statistics` |
| `leaguedashplayerstats` | v2 envelope | `resultSets[].headers[] + resultSets[].rowSet[][]` |
| `leaguestandingsv3` | v2 envelope | `resultSets[0].headers[] + rowSet[][]` |
| `scoreboardv2` | v2 envelope | `resultSets[]` — "GameHeader" (1 row/game) + "LineScore" (1 row/team/game) |

**Gotchas:**
- `leaguegamefinder`: Each row = one team per game. Deduplicate on GAME_ID for unique games. MATCHUP "CHI @ PHI" = CHI away; "PHI vs. CHI" = PHI home.
- `boxscoretraditionalv3`: `minutes` is "MM:SS" string (e.g. "35:56"), not decimal.
- `boxscoreadvancedv3`: `PIE` is all-caps in JSON.
- v2 endpoints return positional arrays — use header index for column lookup.

---

## Worker Jobs

**Startup order (sequential in Program.cs):**
1. `SyncStandingsJob` — single API call
2. `SyncSeasonAveragesJob` — 2 calls per season (traditional + advanced)
3. `HistoricalBackfillJob` — can take hours; resumes from cursor

**Hangfire recurring schedule (UTC):**

| Job | Cron | What it does |
|-----|------|-------------|
| `sync-box-scores` | `0 3 * * *` (3 AM) | Fetches traditional + advanced box scores for yesterday's games |
| `sync-season-averages` | `0 4 * * *` (4 AM) | Refreshes current season player averages |
| `sync-standings` | `0 5 * * *` (5 AM) | Fetches current standings snapshot |

**Season year logic:** `month >= 10 ? currentYear : currentYear - 1` (NBA season spans Oct–Jun)

**Idempotency:** Jobs use `SyncStates` table to track completion per game/date/season.

---

## Frontend Pages & Components

| Page | Route | Data Source |
|------|-------|------------|
| GamePreviewPage | `/` (default) | `GET /api/games/upcoming` + `/api/teams/{id}/matchup/{opponentId}` + `/api/players/season-averages` |
| HotPage | `/hot` | `GET /api/hot/players` + `GET /api/hot/teams` |
| StandingsPage | `/standings` | `GET /api/standings` |

**Key component tree:**
- `GamePreviewPage` → `UpcomingGamesGrid` → `GameCard` (team circles with abbreviations + scores)
- `GamePreviewPage` → `MatchupPanel` → `MatchupScoreSummary` + `MatchupHistoryTable` + `PlayersToWatch`
- `HotPage` → `HotPlayersTable` + `HotTeamsTable` (with `HeatBadge` + `DeltaStat`)
- `StandingsPage` → `StandingsTable` (East/West tabs)

**API client config:** `VITE_API_URL` env var → axios baseURL

---

## Known Issues

### 1. Team Abbreviations Empty in DB
**Status:** Worked around in frontend.
**Problem:** All 30 teams in the `Teams` table have `Abbreviation = ""`. Root cause: teams were created before SyncStandingsJob ran (standings skips teams not in DB), and SyncBoxScoresJob only sets abbreviation when `Conference` is empty (line 286-288), which it isn't after standings runs.
**Workaround:** `GameCard.tsx` falls back to hardcoded `TEAM_INFO` map from `src/data/teams.ts`:
```tsx
{game.visitorTeam.abbreviation || TEAM_INFO[game.visitorTeam.id]?.abbreviation || "?"}
```
**Proper fix:** Either run a one-time SQL UPDATE to populate abbreviations, or fix the worker job ordering so box scores can always set abbreviation.

---

## Testing

**Test project:** `tests/NbaDashboard.Api.Tests/`
**Framework:** xUnit + WebApplicationFactory + FakeNbaStatsClient

**Test files:**
- `GamesControllerTests.cs` — Upcoming games, LineScore fallback, caching, errors
- `TeamsControllerTests.cs` — Matchup history, player stats
- `PlayersControllerTests.cs` — Season averages
- `StartupTests.cs` — DI/startup validation
- `MappingProfileTests.cs` — AutoMapper config
- `ErrorHandlingTests.cs` — Error response shapes

**Run tests:**
```bash
cd /Users/macprom/Desktop/nba/nba-dashboard && /usr/local/share/dotnet/dotnet test
```

**Test data:** Seeded via `Helpers/TestDataSeeder.cs` (Celtics, Lakers, sample players/games).

---

## Quick Reference Commands

```bash
# API health check
curl -s http://localhost:5000/api/games/upcoming | python3 -m json.tool | head -40

# Query DB (from host, via Docker)
docker exec nba-dashboard-db-1 psql -U nba_user -d nba_dashboard -c 'SELECT "Id", "Abbreviation", "Name" FROM "Teams" LIMIT 10;'

# List all tables
docker exec nba-dashboard-db-1 psql -U nba_user -d nba_dashboard -c '\dt'

# Check running containers
docker ps --format "table {{.Names}}\t{{.Ports}}" | grep -i nba

# Run .NET tests
cd /Users/macprom/Desktop/nba/nba-dashboard && /usr/local/share/dotnet/dotnet test

# Rebuild specific container
docker compose -f /Users/macprom/Desktop/nba/nba-dashboard/docker-compose.yml up -d --build frontend
```
