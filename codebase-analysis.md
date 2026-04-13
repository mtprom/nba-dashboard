# NBA Dashboard Codebase Analysis

Updated: 2026-04-08 (includes changes through today)

Purpose: this is an operational reference for agent-assisted coding in this repository. It is written for Codex, Claude Code, and humans who need a fast, accurate model of how the system is assembled, where behavior lives, and how to make changes safely.

## 1. Workspace Shape

Repository root:

- `/Users/macprom/Desktop/nba`

Actual application root:

- `/Users/macprom/Desktop/nba/nba-dashboard`

Top-level docs at repo root:

- `README.md`: very short project summary
- `CODEBASE_GUIDE.md`: broad file walkthrough, partially useful but more descriptive than operational
- `NBA_Analytics_Dashboard_Architecture.md`: architecture intent, but several parts no longer match implementation
- `codebase-analysis.md`: this file

Most coding work should happen inside:

- `/Users/macprom/Desktop/nba/nba-dashboard`

Operational environment note:

- The real Dockerized app runs on a separate Linux server that you access over SSH.
- This project is not currently deployed to a public cloud or hosted production platform.
- Local code edits happen on this machine, but full runtime verification may need to happen on the remote Linux box.

## 2. What The System Does

This project ingests NBA data from `stats.nba.com`, stores normalized game and season data in PostgreSQL, exposes read-oriented API endpoints from ASP.NET Core, and renders that data in a React frontend.

There are three main runtime surfaces:

1. API
   - Serves JSON to the frontend.
   - Reads only from the database and in one place indirectly benefits from cached scoreboard payloads.

2. Worker
   - Pulls from NBA endpoints using a custom client.
   - Upserts seasons, teams, players, games, box scores, standings, season averages, and cached scoreboard payloads.

3. Frontend
   - Calls the API.
   - Handles presentation, routing, tables/charts, and some fallback display logic such as hardcoded team metadata.

## 3. Current Stack

Backend:

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core 8
- PostgreSQL 16
- AutoMapper
- Hangfire + Hangfire.PostgreSql
- In-memory caching via `IMemoryCache`

Frontend:

- React 18
- TypeScript
- Vite 5
- Tailwind CSS
- Recharts
- axios
- Radix UI primitives for a few UI pieces

Testing:

- xUnit
- `WebApplicationFactory`
- SQLite in-memory test database
- Fake `NbaStatsClient`

Containers:

- `docker-compose.yml` runs `db`, `api`, `worker`, and `frontend`
- API exposed on `localhost:5000`
- Frontend exposed on `localhost:5173`

Operationally:

- those ports describe the compose stack itself, but the actual long-running stack is on the remote Linux server, not necessarily on the local workstation

## 4. High-Level Dependency Graph

Logical project dependency chain:

`NbaDashboard.Core` <- `NbaDashboard.Infrastructure` <- (`NbaDashboard.Api`, `NbaDashboard.Worker`)

Frontend is independent of the .NET projects and only depends on API contracts.

Important practical implication:

- Domain/entity changes usually ripple from `Core` to `Infrastructure` to API DTOs/controllers and then to frontend types/components.

## 5. Runtime Data Flow

Main ingestion path:

1. `NbaDashboard.Worker` schedules or runs jobs.
2. Jobs call `NbaStatsClient`.
3. `NbaStatsClient` shells out to `curl` against `https://stats.nba.com/stats/...`.
4. Worker deserializes endpoint-specific models in `NbaDashboard.Infrastructure/NbaStats/Models`.
5. Worker upserts EF entities into PostgreSQL through `AppDbContext`.
6. API controllers query the database and shape responses into DTOs.
7. Frontend pages call `/api/...` endpoints via axios and render cards, tables, and charts.

Important ingestion invariants:

- Box score sync is now strict: a game is only considered complete once the `Games` row exists and both `PlayerGameStats` and `PlayerGameAdvanced` contain matching player coverage for that `gameId`.
- Successful game completion is tracked with `SyncStates.Key = boxscore_{gameId}`.
- Incomplete or retryable game syncs are tracked separately with `SyncStates.Key = boxscore_retry_{gameId}`.
- The recurring ongoing sync no longer checks only one day; `SyncBoxScoresJob.RunAsync(null)` sweeps the most recent 3 completed UTC dates so delayed NBA payload availability can self-heal without manual reruns.
- Historical season backfill does not mark `backfill_season_{year}` complete unless a post-run coverage audit confirms that every expected regular-season `GAME_ID` for that season is fully present.

Scoreboard path has an extra cache layer:

1. `PreWarmScoreboardJob` fetches `scoreboardv2` every 15 minutes.
2. It stores raw serialized payloads in `CachedScoreboards`.
3. `GamesController.GetUpcoming()` first checks in-memory cache, then database cache, then NBA API live fetch.

## 6. Important Directories

### `/Users/macprom/Desktop/nba/nba-dashboard/NbaDashboard.Core`

Pure entity classes. No external I/O. This is the domain schema layer.

Key entities:

- `Season`
- `Team`
- `Player`
- `Game`
- `PlayerGameStats`
- `PlayerGameAdvanced`
- `PlayerSeasonStats`
- `PlayerHeat`
- `StandingsSnapshot`
- `SyncState`
- `CachedScoreboard`

### `/Users/macprom/Desktop/nba/nba-dashboard/NbaDashboard.Infrastructure`

Infrastructure and data-access layer.

Key files:

- [AppDbContext.cs](/Users/macprom/Desktop/nba/nba-dashboard/NbaDashboard.Infrastructure/Data/AppDbContext.cs)
- [NbaStatsClient.cs](/Users/macprom/Desktop/nba/nba-dashboard/NbaDashboard.Infrastructure/NbaStats/NbaStatsClient.cs)
- [NbaStatsHeaders.cs](/Users/macprom/Desktop/nba/nba-dashboard/NbaDashboard.Infrastructure/NbaStats/NbaStatsHeaders.cs)
- endpoint response models in `NbaStats/Models`
- EF migrations in `Migrations`

### `/Users/macprom/Desktop/nba/nba-dashboard/NbaDashboard.Api`

Read-side HTTP API.

Key files:

- [Program.cs](/Users/macprom/Desktop/nba/nba-dashboard/NbaDashboard.Api/Program.cs)
- controllers in `Controllers/`
- mapping and DTOs in `DTOs/`

### `/Users/macprom/Desktop/nba/nba-dashboard/NbaDashboard.Worker`

Background ingestion and pre-warming.

Key files:

- [Program.cs](/Users/macprom/Desktop/nba/nba-dashboard/NbaDashboard.Worker/Program.cs)
- jobs in `Jobs/`

Operationally important worker behavior:

- `PreWarmScoreboardJob` only caches today’s `scoreboardv2` payload into `CachedScoreboards`; it is not the authoritative game-history ingestion path.
- `SyncBoxScoresJob` is the authoritative path for completed regular-season games in both rolling sync and historical backfill modes.
- `SyncStandingsJob` now upserts teams directly from standings rows, which avoids the fresh-database failure mode where standings were previously skipped because teams were not seeded yet.

### `/Users/macprom/Desktop/nba/nba-dashboard/nba-frontend`

React client.

Key files:

- [App.tsx](/Users/macprom/Desktop/nba/nba-dashboard/nba-frontend/src/App.tsx)
- `src/pages/*`
- `src/components/*`
- `src/api/*`
- `src/types/*`
- `src/data/teams.ts`

### `/Users/macprom/Desktop/nba/nba-dashboard/tests/NbaDashboard.Api.Tests`

API and startup tests.

Most useful safety net when changing controller behavior.

## 7. API Surface That Actually Exists

Current controllers:

- [GamesController.cs](/Users/macprom/Desktop/nba/nba-dashboard/NbaDashboard.Api/Controllers/GamesController.cs)
- [TeamsController.cs](/Users/macprom/Desktop/nba/nba-dashboard/NbaDashboard.Api/Controllers/TeamsController.cs)
- [PlayersController.cs](/Users/macprom/Desktop/nba/nba-dashboard/NbaDashboard.Api/Controllers/PlayersController.cs)
- [StandingsController.cs](/Users/macprom/Desktop/nba/nba-dashboard/NbaDashboard.Api/Controllers/StandingsController.cs)
- [HotController.cs](/Users/macprom/Desktop/nba/nba-dashboard/NbaDashboard.Api/Controllers/HotController.cs)
- [HistoryController.cs](/Users/macprom/Desktop/nba/nba-dashboard/NbaDashboard.Api/Controllers/HistoryController.cs)

Implemented endpoints:

- `GET /api/games/upcoming`
- `GET /api/teams/{teamId}/matchup/{opponentId}`
- `GET /api/players/season-averages?teamIds=...`
- `GET /api/standings?season=...`
- `GET /api/hot/players?window=5|10|season`
- `GET /api/hot/teams?window=5|10|season`
- `GET /api/history?teamId=&fromSeason=&toSeason=`

Important response shaping:

- DTO mapping is centralized in [MappingProfile.cs](/Users/macprom/Desktop/nba/nba-dashboard/NbaDashboard.Api/DTOs/MappingProfile.cs) for a subset of types.
- Several controllers also manually project DTOs inline.
- Response contracts are duplicated in frontend TypeScript types. Backend DTO changes usually require frontend type changes.

## 8. Frontend Surface That Actually Exists

Routes in [App.tsx](/Users/macprom/Desktop/nba/nba-dashboard/nba-frontend/src/App.tsx):

- `/` -> `HistoryPage`
- `/games` -> `GamePreviewPage`
- `/standings` -> `StandingsPage`
- `/hot` -> `HotPage`
- `/history` -> `HistoryPage`

Frontend fetch layer:

- [client.ts](/Users/macprom/Desktop/nba/nba-dashboard/nba-frontend/src/api/client.ts): axios base URL from `VITE_API_URL`
- `games.ts`, `standings.ts`, `hot.ts`, `history.ts`: thin wrappers around API calls

Practical frontend architecture:

- Pages own fetch orchestration and page-level state.
- Presentational logic is pushed into components.
- Team branding and fallback metadata live in `src/data/teams.ts`.
- Header brand text is currently `.netBall`, not `NBA Dashboard`.

Notable page behavior:

- `GamePreviewPage` loads upcoming games, then bulk-loads season averages for all teams on the slate, and lazily loads matchup history when a game is selected.
- `HistoryPage` is analytics-heavy and depends on a single aggregated API endpoint instead of several smaller ones.
- `HistoryPage` is also the default landing page for `/`; Today’s Games lives at `/games`.
- `HotPage` depends on backend-computed ranking logic, not client-side calculations.
- `HotPage` semantics are split by window:
  - `window=5|10` compares the latest N games against earlier games from the same season and requires at least N prior games.
  - `window=season` compares this season against last season.
- Team-specific history visuals use `getTeamColorsDark(...)` as the source of truth for dark-mode-safe branding.

## 9. Database Model And Persistence Rules

The EF model is defined in [AppDbContext.cs](/Users/macprom/Desktop/nba/nba-dashboard/NbaDashboard.Infrastructure/Data/AppDbContext.cs).

Important schema rules:

- `Team.Id` and `Player.Id` are `ValueGeneratedNever()`
  - These IDs come directly from NBA upstream data.
- `Game.Id` is a string NBA game ID.
- Unique indexes protect:
  - `(GameId, PlayerId)` for `PlayerGameStats`
  - `(GameId, PlayerId)` for `PlayerGameAdvanced`
  - `(PlayerId, SeasonId, TeamId)` for `PlayerSeasonStats`
  - `(PlayerId, ComputedDate)` for `PlayerHeat`
  - `(TeamId, SeasonId, SnapshotDate)` for `StandingsSnapshot`
  - `Date` for `CachedScoreboard`

Important persistence behavior:

- API is read-only with respect to business data.
- Worker is the only intended writer of NBA-derived data.
- API startup auto-runs EF migrations unless environment is `Testing`.

Agent implication:

- If you add fields to entities, also inspect:
  - EF model configuration
  - migrations
  - worker upsert logic
  - controller projections
  - DTOs
  - frontend types/components
  - test seed data

## 10. Worker Jobs And Scheduling

Worker startup in [Program.cs](/Users/macprom/Desktop/nba/nba-dashboard/NbaDashboard.Worker/Program.cs):

On boot it immediately runs:

1. standings replay for the full configured season range
2. season averages sync
3. historical backfill

Then it schedules recurring Hangfire jobs:

- `sync-box-scores` -> `0 3 * * *`
- `sync-season-averages` -> `0 4 * * *`
- `sync-standings` -> `0 5 * * *`
- `prewarm-scoreboard` -> `*/15 * * * *`

Important jobs:

- [SyncBoxScoresJob.cs](/Users/macprom/Desktop/nba/nba-dashboard/NbaDashboard.Worker/Jobs/SyncBoxScoresJob.cs)
  - Uses `leaguegamefinder` to discover expected `GAME_ID`s for a date or season before deciding what to skip.
  - Recurring `RunAsync(null)` now sweeps the most recent 3 completed UTC dates instead of only yesterday.
  - Fetches `boxscoretraditionalv3` and `boxscoreadvancedv3` per incomplete game.
  - Upserts seasons, teams, players, game rows, and player game stat rows.
  - Treats a game as complete only when the `Games` row exists and traditional/advanced player coverage matches.
  - Repairs stale old sync markers: if `boxscore_{gameId}` exists but coverage is incomplete, the job removes trust in that marker, records `boxscore_retry_{gameId}`, and retries the game.
  - Returns season/date coverage audits so backfill completion is based on verified coverage, not only iteration.

- `SyncSeasonAveragesJob`
  - Refreshes player season-level averages and advanced metrics.
  - Completed-season cursors must not be trusted when a season has zero `PlayerSeasonStats` rows; replay that season instead of skipping it.
  - Season averages should run after historical box-score backfill on startup, because missing player/team rows cause season-average upserts to be skipped.

- `SyncStandingsJob`
  - Merges `leaguestandingsv3` with `leaguedashteamstats` (`MeasureType=Advanced`) by `TEAM_ID`.
  - Uses `leaguestandingsv3` for ranks, records, streaks, conference/division metadata.
  - Uses `leaguedashteamstats` as the canonical source for `OFF_RATING`, `DEF_RATING`, `NET_RATING`, and `PACE`.
  - Replays standings across the full `NbaStats:BackfillStartDate` range on startup.
  - Treats latest all-zero advanced ratings in `StandingsSnapshots` as stale and replays that season even if a sync marker exists.
  - Reuses one stable snapshot date for completed seasons so historical replay updates existing final standings instead of creating duplicate dates.

- `HistoricalBackfillJob`
  - Long-running historical ingest.
  - Only writes `backfill_season_{year}` after the season coverage audit confirms every expected regular-season game is complete.

- [PreWarmScoreboardJob.cs](/Users/macprom/Desktop/nba/nba-dashboard/NbaDashboard.Worker/Jobs/PreWarmScoreboardJob.cs)
  - Stores serialized daily scoreboard JSON in `CachedScoreboards`.

Agent implication:

- Most data quality problems originate in worker upsert rules, not in controllers.
- If API output is wrong but database rows are already wrong, fix the worker or backfill path rather than only patching the controller.
- One current exception: `HistoryController` now defensively filters obviously bad “completed” rows at read time (for example future-dated `Final` games or `0-0` score rows) so the history UI is not polluted by bad data.

## 11. NBA Client Reality

The biggest implementation surprise is [NbaStatsClient.cs](/Users/macprom/Desktop/nba/nba-dashboard/NbaDashboard.Infrastructure/NbaStats/NbaStatsClient.cs).

Despite being registered with `AddHttpClient`, it does not use `HttpClient` for requests. It:

- ignores the injected `HttpClient`
- shells out to `curl`
- adds headers from `NbaStatsHeaders.Default`
- enforces a static global throttle with `SemaphoreSlim`
- sleeps 6000 to 7000 ms after each request

This matters because:

- tests avoid real network by overriding `GetAsync`
- runtime throughput is intentionally slow
- troubleshooting should include `curl` availability inside the runtime environment
- changing timeout or handler configuration in DI does not affect the actual request path nearly as much as expected

## 12. Caching Layers

There are two caches in play:

1. API memory cache
   - `GamesController`: `scoreboard_today`
   - `TeamsController`: matchup cache for 4 hours
   - `PlayersController`: season averages for 1 hour
   - `HotController`: 5 minutes
   - `HistoryController`: 5 minutes

2. Database-backed cached scoreboard
   - table: `CachedScoreboards`
   - producer: `PreWarmScoreboardJob`
   - consumer: `GamesController`

Agent implication:

- When debugging stale API responses, inspect both memory-cache behavior and `CachedScoreboards`.
- Controller tests may pass while production behavior stays stale because the tests do not simulate all cache layers equally.

## 13. Where Business Logic Lives

This codebase is not heavily service-layered. Logic is distributed as follows:

- Worker jobs contain ingestion, transformation, and upsert logic.
- Controllers contain aggregation, filtering, and response-shaping logic.
- DTO mapping is split between AutoMapper and manual projections.
- Frontend pages coordinate fetches and local state.

There is very little abstraction between controller and EF query.

That means:

- small changes are usually easy
- larger refactors require care because behavior is not centralized
- duplicated logic can exist across controllers and worker jobs

## 14. Known Coupling And Risk Areas

### 14.1 Team identity data can be incomplete

`SyncBoxScoresJob.UpsertTeamAsync()` only updates name/city/abbreviation when `team.Conference` is empty. Once standings have populated canonical team metadata, later box-score syncs stop refreshing those fields.

Practical effect:

- the long-standing “missing team abbreviation” issue is plausible and the existing docs about it are directionally correct
- frontend already contains fallback team metadata in `src/data/teams.ts`

### 14.2 Controller logic assumes specific upstream schemas

`GamesController` depends on `GameHeader` and `LineScore` shapes from `scoreboardv2`.

If upstream headers change:

- parsing may silently degrade
- empty arrays are often returned instead of hard failures

### 14.3 History endpoint has opinionated read-time cleanup

`HistoryController` now excludes games that are:

- not `Final`
- postseason
- future-dated
- `0-0`

Practical effect:

- the history UI is intentionally protected from obviously invalid completed-game rows
- if the database contains bad “final” records, history pages may disagree with raw table contents by design
- if someone changes these filters, they are changing product semantics, not just query optimization

### 14.4 Season logic is duplicated

Several places compute current season with variants of:

- `now.Month >= 10 ? now.Year : now.Year - 1`

If season-boundary behavior changes, search broadly.

### 14.5 SQLite tests are not PostgreSQL-perfect

Tests run on SQLite in-memory, production uses PostgreSQL.

Watch for:

- SQL translation differences
- date/time semantics
- index and uniqueness edge cases
- provider-specific behavior

### 14.6 Startup behavior is heavy

Worker boot immediately triggers real data work, including historical backfill.

That means:

- container startup can be expensive
- local “just run the worker” behavior is not lightweight
- edits to worker startup should be treated as operational changes, not only code changes

### 14.7 Standings advanced metrics source-of-truth

As confirmed against the remote Linux server on 2026-04-08:

- live `leaguestandingsv3` payloads do not expose team advanced ratings in the shape the old worker expected
- the live payload includes standings/record fields plus scoring-style fields such as `PointsPG`, `OppPointsPG`, and `DiffPointsPG`
- the canonical team advanced metrics (`OFF_RATING`, `DEF_RATING`, `NET_RATING`, `PACE`) are available from `leaguedashteamstats` with `MeasureType=Advanced`
- `StandingsSnapshots.OffRating/DefRating/NetRating/Pace` should therefore be populated from `leaguedashteamstats`, not inferred from `leaguestandingsv3`

Practical implication:

- if standings ratings are zero again, first inspect whether `leaguedashteamstats` returned the canonical advanced headers and whether the latest `StandingsSnapshots` rows are all-zero
- do not start debugging this in the frontend unless the database rows are already correct

## 15. Testing Reality

Test harness:

- [TestWebApplicationFactory.cs](/Users/macprom/Desktop/nba/nba-dashboard/tests/NbaDashboard.Api.Tests/Fixtures/TestWebApplicationFactory.cs)
  - swaps PostgreSQL for SQLite in-memory
  - replaces real `NbaStatsClient` with `FakeNbaStatsClient`
  - seeds test data once per factory instance

Current test coverage is strongest around:

- API startup wiring
- AutoMapper config
- games endpoint behavior and caching
- history endpoint behavior now has focused tests for invalid final rows and team-mode win-share semantics
- matchup endpoint
- players endpoint
- error handling and proxy/CORS-shaped requests
- worker ingestion behavior now has focused tests for:
  - null traditional response does not mark a game complete
  - null advanced response leaves the game retryable
  - complete traditional + advanced responses create final sync state
  - historical backfill does not mark a season complete when coverage is still incomplete
  - standings sync can upsert teams into a fresh DB

Current test coverage is weaker around:

- worker jobs
- EF migration correctness under PostgreSQL
- frontend behavior
- full end-to-end containerized flow
- historical analytics correctness in edge cases
- hot rankings correctness in edge cases

Agent implication:

- if you change worker logic, you may need to add tests from scratch
- if you change controller output contracts, backend tests alone are not enough; inspect frontend consumers

## 16. Fast File Map For Common Tasks

If the task is “change an API response”:

- controller in `NbaDashboard.Api/Controllers`
- DTO in `NbaDashboard.Api/DTOs`
- mapping in `MappingProfile.cs` if relevant
- frontend types in `nba-frontend/src/types`
- frontend API wrapper in `nba-frontend/src/api`
- page/component consumers
- API tests

If the task is “fix wrong NBA data in DB”:

- worker job
- upstream response model if parsing is wrong
- entity definitions and `AppDbContext`
- consider whether backfill/resync behavior is needed

If the task is “add a new page or chart”:

- backend endpoint first if data does not already exist
- frontend `src/api`
- frontend `src/types`
- page in `src/pages`
- display components in `src/components`

If the task is “fix standings/team/player identity issues”:

- `SyncStandingsJob`
- `SyncBoxScoresJob`
- `Team` / `Player` entity shape
- any frontend fallback metadata in `src/data/teams.ts`

If the task is “fix history page analytics or visuals”:

- `HistoryController` is the source of truth for filtering and analytics semantics
- `HistoryPage` passes team-color context into team-specific visuals
- `SeasonIdentityTags` now uses a responsive grid rather than horizontal scrolling or loose flex-wrap
- `MonthlyHeatmap` uses a neutral-gray-to-team-color ramp, not a generic blue palette

## 17. Recommended Agent Workflow

For backend changes:

1. Read the target controller or job first.
2. Read the DTOs/types it returns or consumes.
3. Check entity definitions and `AppDbContext` if persistence is involved.
4. Search for matching frontend consumers before editing contracts.
5. Run focused tests before broader test runs.

For frontend changes:

1. Read the page file first.
2. Read the API wrapper and TS types.
3. Inspect reusable components already in the route subtree.
4. Preserve current visual language unless intentionally redesigning.

For data bugs:

1. Decide whether the bug is in source fetch, transform, persistence, or read model.
2. Verify whether controller logic is only exposing already-bad rows.
3. Avoid “presentation-only” fixes when ingestion is the root cause.

## 18. Safe Edit Zones Vs Risky Edit Zones

Usually safe:

- adding a frontend-only component
- changing labels/layout/styling without contract changes
- adjusting controller-side projection fields if fully covered by tests and frontend updates
- adding new read-only endpoints

Higher risk:

- entity changes
- migrations
- worker upsert logic
- season/date handling
- `NbaStatsClient`
- startup and scheduling behavior
- team/player identity synchronization

## 19. Practical Commands

From app root:

```bash
cd /Users/macprom/Desktop/nba/nba-dashboard
```

Run backend tests:

```bash
dotnet test
```

Run a focused backend test project:

```bash
dotnet test tests/NbaDashboard.Api.Tests/NbaDashboard.Api.Tests.csproj
```

Run frontend build:

```bash
cd /Users/macprom/Desktop/nba/nba-dashboard/nba-frontend
npm run build
```

Run local stack:

```bash
cd /Users/macprom/Desktop/nba/nba-dashboard
docker compose up --build
```

Relevant ports from current compose file:

- API: `http://localhost:5000`
- Frontend: `http://localhost:5173`
- Postgres: `localhost:5432`

Remote-ops caveat:

- treat the local compose commands as development commands, not as a description of the live environment
- if a task involves checking real running containers, logs, database state, or live API behavior, that verification likely belongs on the SSH-accessed Linux server

## 20. Suggested First Reads For A New Agent

Read in this order:

1. [NbaDashboard.Api/Program.cs](/Users/macprom/Desktop/nba/nba-dashboard/NbaDashboard.Api/Program.cs)
2. [NbaDashboard.Worker/Program.cs](/Users/macprom/Desktop/nba/nba-dashboard/NbaDashboard.Worker/Program.cs)
3. [AppDbContext.cs](/Users/macprom/Desktop/nba/nba-dashboard/NbaDashboard.Infrastructure/Data/AppDbContext.cs)
4. [GamesController.cs](/Users/macprom/Desktop/nba/nba-dashboard/NbaDashboard.Api/Controllers/GamesController.cs)
5. [HistoryController.cs](/Users/macprom/Desktop/nba/nba-dashboard/NbaDashboard.Api/Controllers/HistoryController.cs)
6. [SyncBoxScoresJob.cs](/Users/macprom/Desktop/nba/nba-dashboard/NbaDashboard.Worker/Jobs/SyncBoxScoresJob.cs)
7. [NbaStatsClient.cs](/Users/macprom/Desktop/nba/nba-dashboard/NbaDashboard.Infrastructure/NbaStats/NbaStatsClient.cs)
8. [nba-frontend/src/App.tsx](/Users/macprom/Desktop/nba/nba-dashboard/nba-frontend/src/App.tsx)
9. [nba-frontend/src/pages/GamePreviewPage.tsx](/Users/macprom/Desktop/nba/nba-dashboard/nba-frontend/src/pages/GamePreviewPage.tsx)
10. [tests/NbaDashboard.Api.Tests/Fixtures/TestWebApplicationFactory.cs](/Users/macprom/Desktop/nba/nba-dashboard/tests/NbaDashboard.Api.Tests/Fixtures/TestWebApplicationFactory.cs)

## 21. Bottom Line

This is a pragmatic, moderately coupled full-stack app with:

- thin API layers
- heavy logic in controllers and worker jobs
- a custom slow-throttled upstream client
- a useful but incomplete backend test suite
- a frontend that depends on stable DTO contracts and occasionally compensates for imperfect backend data

For agent coding, the winning strategy is:

- trace data end-to-end before editing
- treat worker code as the source of truth for persistent data quality
- assume docs may be stale unless confirmed against code
- update backend tests and frontend types together when changing contracts
