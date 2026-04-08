# .netBall

`.netBall` is an NBA analytics dashboard powered by `stats.nba.com`.

It combines a .NET 8 Web API, a .NET worker for data ingestion, PostgreSQL for storage, and a React/Vite frontend for the UI. The app focuses on upcoming games, matchup context, standings, hot teams and players, and team history views built from ingested NBA data.

Development happens locally in this repo, while the real Dockerized stack runs on a separate Linux server over SSH. The main application code lives in `/Users/macprom/Desktop/nba/nba-dashboard`, and shared agent context is maintained in `/Users/macprom/Desktop/nba/codebase-analysis.md`.

## Stack

- ASP.NET Core 8 Web API
- .NET worker service for NBA data ingestion
- PostgreSQL with Entity Framework Core 8
- React 18 + Vite + TypeScript + Tailwind CSS
- Recharts for analytics visualizations

## Notes

- The live backend is not deployed to a public cloud environment right now.
- Local changes in this repo do not affect the running server until they are deployed to the separate Linux box.
