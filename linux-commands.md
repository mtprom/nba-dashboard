# NBA Dashboard - Linux Server Commands

All commands assume you're in `~/nba-dashboard/nba-dashboard`.

## Docker Lifecycle

```bash
# Start everything
docker compose up -d db && sleep 10 && docker compose up -d api worker

# Stop everything (containers preserved)
docker compose stop

# Start again after stop (resumes from cursor)
docker compose start

# Tear down completely (containers removed, data preserved in volume)
docker compose down

# Tear down AND delete all data (nuclear option)
docker compose down -v
```

## Worker (Backfill)

```bash
# Watch live logs
docker compose logs -f worker

# Last 50 lines
docker compose logs worker --tail=50

# Restart worker (resumes from last synced game)
docker compose restart worker

# Stop only the worker
docker compose stop worker

# Start only the worker
docker compose start worker

# Rebuild after code change
git pull && docker compose build worker && docker compose up -d worker
```

## Database Queries

```bash
# Enter psql
docker compose exec db psql -U nba_user -d nba_dashboard
```

Once inside psql:

```sql
-- List all tables
\dt

-- Backfill progress (which seasons are done)
SELECT * FROM "SyncStates" WHERE "Key" LIKE 'backfill_season_%';

-- Total counts
SELECT COUNT(*) AS games FROM "Games";
SELECT COUNT(*) AS player_stats FROM "PlayerGameStats";
SELECT COUNT(*) AS advanced_stats FROM "PlayerGameAdvanced";
SELECT COUNT(*) AS players FROM "Players";
SELECT COUNT(*) AS teams FROM "Teams";

-- Top 20 scoring performances
SELECT p."FirstName", p."LastName", s."Points", s."Rebounds", s."Assists", g."Date"
FROM "PlayerGameStats" s
JOIN "Players" p ON p."Id" = s."PlayerId"
JOIN "Games" g ON g."Id" = s."GameId"
ORDER BY s."Points" DESC
LIMIT 20;

-- Games per season
SELECT se."Year", COUNT(*) AS games
FROM "Games" g
JOIN "Seasons" se ON se."Id" = g."SeasonId"
GROUP BY se."Year"
ORDER BY se."Year";

-- Recent games with scores
SELECT g."Date", h."Abbreviation" AS home, g."HomeScore",
       v."Abbreviation" AS away, g."VisitorScore"
FROM "Games" g
JOIN "Teams" h ON h."Id" = g."HomeTeamId"
JOIN "Teams" v ON v."Id" = g."VisitorTeamId"
ORDER BY g."Date" DESC
LIMIT 20;

-- How many games have been synced total
SELECT COUNT(*) FROM "SyncStates" WHERE "Key" LIKE 'boxscore_%';

-- Exit psql
\q
```

One-liner queries (no need to enter psql):

```bash
# Quick game count
docker compose exec db psql -U nba_user -d nba_dashboard -c 'SELECT COUNT(*) FROM "Games";'

# Backfill season status
docker compose exec db psql -U nba_user -d nba_dashboard -c 'SELECT * FROM "SyncStates" WHERE "Key" LIKE '\''backfill_%'\'';'
```

## Monitoring

```bash
# Aggregated success/failure counts (all time)
docker logs nba-dashboard-worker-1 2>&1 | awk '/Synced /{s++} /Failed to sync/{f++} END{print "Synced: "s+0, "| Failed: "f+0, "| Total: "s+0+f+0}'

# Aggregated success/failure counts (last hour)
docker logs --since 1h nba-dashboard-worker-1 2>&1 | awk '/Synced /{s++} /Failed to sync/{f++} END{print "Synced: "s+0, "| Failed: "f+0, "| Total: "s+0+f+0}'

# Recent failures with game IDs and error reasons
docker logs nba-dashboard-worker-1 2>&1 | grep -A2 "Failed to sync" | tail -30

# Most recently synced games
docker exec -i nba-dashboard-db-1 psql -U nba_user -d nba_dashboard -c 'SELECT "Key", "UpdatedAt" FROM "SyncStates" WHERE "Key" LIKE '\''boxscore_%'\'' ORDER BY "UpdatedAt" DESC LIMIT 10;'

# Season completion progress
docker logs nba-dashboard-worker-1 2>&1 | grep -E "Season .* complete" | tail -20

# Recent games with scores and teams
docker exec -i nba-dashboard-db-1 psql -U nba_user -d nba_dashboard -c 'SELECT g."Date", h."Abbreviation" AS home, g."HomeScore", v."Abbreviation" AS away, g."VisitorScore" FROM "Games" g JOIN "Teams" h ON g."HomeTeamId" = h."Id" JOIN "Teams" v ON g."VisitorTeamId" = v."Id" ORDER BY g."Date" DESC LIMIT 10;'
```

## Deploying Code Changes

```bash
git pull
docker compose down
docker compose build
docker compose up -d db && sleep 10 && docker compose up -d api worker
```

## Email Alerts (msmtp)

```bash
# Test email
echo -e "Subject: Test\nFrom: macprom06@gmail.com\nTo: macprom06@gmail.com\n\nTest" | msmtp macprom06@gmail.com

# Check msmtp log
cat ~/.msmtp.log

# View cron jobs
crontab -l

# Reset alert flag (stops repeated emails until next failure)
rm -f /tmp/nba-alert-sent
```

## Troubleshooting

```bash
# Check container status
docker compose ps

# Check worker errors only
docker compose logs worker --tail=100 2>&1 | grep -i error

# Check if blocked by NBA API (curl exit 28 = timeout = likely IP banned)
docker logs --since 30m nba-dashboard-worker-1 2>&1 | grep "curl failed"

# Fix IP ban: restart router or wait 15-30 min, then restart worker
docker compose restart worker

# Check disk space
df -h

# Check docker volume size
docker system df -v
```
