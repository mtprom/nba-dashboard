# nba-dashboard

NBA stats dashboard built on top of stats.nba.com. Pulls real game data, box scores, and advanced metrics and displays them in a web UI.

## Stack

- **Backend:** ASP.NET Core 8 Web API
- **Database:** PostgreSQL 16 via Entity Framework Core
- **Ingestion:** .NET Worker Service curl pulling from stats.nba.com
- **Frontend:** React 18 + Vite + Recharts
- **Deployment:** Docker Compose (local), AWS (prod target)

## Status

Work in progress.
