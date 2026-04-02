import { useMemo } from "react"
import PlayerOutperformCard from "./PlayerOutperformCard"
import { getTeamColorsDark } from "@/data/teams"
import type { MatchupHistory, MatchupGame, PlayerSeasonAvg, OutperformingPlayer } from "@/types"

interface PlayersToWatchProps {
  matchup: MatchupHistory
  seasonAverages: Record<number, PlayerSeasonAvg>
}

function getCurrentSeasonYear(): number {
  const now = new Date()
  return now.getMonth() >= 9 ? now.getFullYear() : now.getFullYear() - 1
}

function getLastSeasonGame(matchup: MatchupHistory): MatchupGame | null {
  const seasonYear = getCurrentSeasonYear()
  const seasonStart = `${seasonYear}-10-01`
  // games are ordered newest-first from the API
  return matchup.games.find((mg) => mg.game.date >= seasonStart) ?? null
}

function computeOutperformers(
  game: MatchupGame,
  seasonAverages: Record<number, PlayerSeasonAvg>
): OutperformingPlayer[] {
  const allStats = [
    ...game.homePlayerStats.map((ps) => ({ ps, teamId: game.homeTeam.id })),
    ...game.visitorPlayerStats.map((ps) => ({ ps, teamId: game.visitorTeam.id })),
  ]

  const outperformers: OutperformingPlayer[] = []

  for (const { ps, teamId } of allStats) {
    const sa = seasonAverages[ps.playerId]
    if (!sa) continue

    const vsAvg = {
      ptsAvg: ps.points,
      rebAvg: ps.rebounds,
      astAvg: ps.assists,
      fgPct: ps.fieldGoalPct,
      gamesPlayed: 1,
    }

    const delta = {
      pts: vsAvg.ptsAvg - sa.ptsAvg,
      reb: vsAvg.rebAvg - sa.rebAvg,
      ast: vsAvg.astAvg - sa.astAvg,
      fgPct: vsAvg.fgPct - sa.fgPct,
    }

    const significant =
      delta.pts >= 3 || delta.reb >= 2 || delta.ast >= 2 || delta.fgPct >= 0.03

    if (significant) {
      outperformers.push({
        playerId: ps.playerId,
        playerName: sa.playerName,
        position: sa.position,
        jerseyNumber: sa.jerseyNumber,
        teamId,
        vsOpponent: vsAvg,
        seasonAvg: {
          ptsAvg: sa.ptsAvg,
          rebAvg: sa.rebAvg,
          astAvg: sa.astAvg,
          fgPct: sa.fgPct,
          gamesPlayed: sa.gamesPlayed,
        },
        delta,
      })
    }
  }

  return outperformers.sort((a, b) => b.delta.pts - a.delta.pts)
}

export default function PlayersToWatch({ matchup, seasonAverages }: PlayersToWatchProps) {
  const lastSeasonGame = useMemo(() => getLastSeasonGame(matchup), [matchup])

  const outperformers = useMemo(
    () => (lastSeasonGame ? computeOutperformers(lastSeasonGame, seasonAverages) : []),
    [lastSeasonGame, seasonAverages]
  )

  if (!lastSeasonGame) {
    return (
      <div className="py-8 text-center text-sm text-muted-foreground">
        No matchup this season.
      </div>
    )
  }

  if (outperformers.length === 0) {
    return (
      <div className="py-8 text-center text-sm text-muted-foreground">
        No standouts last matchup.
      </div>
    )
  }

  return (
    <div>
      <p className="mb-4 text-sm text-muted-foreground">
        Players who significantly outperformed their season averages in the last matchup.
      </p>
      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
        {outperformers.map((player) => (
          <PlayerOutperformCard
            key={player.playerId}
            player={player}
            teamColor={getTeamColorsDark(player.teamId).primary}
          />
        ))}
      </div>
    </div>
  )
}
