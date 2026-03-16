import { useMemo } from "react"
import PlayerOutperformCard from "./PlayerOutperformCard"
import { getTeamColors } from "@/data/teams"
import type { MatchupHistory, PlayerSeasonAvg, OutperformingPlayer, PlayerGameStats } from "@/types"

interface PlayersToWatchProps {
  matchup: MatchupHistory
  seasonAverages: Record<number, PlayerSeasonAvg>
}

function computeOutperformers(
  matchup: MatchupHistory,
  seasonAverages: Record<number, PlayerSeasonAvg>
): OutperformingPlayer[] {
  const playerGames: Record<number, { stats: PlayerGameStats[]; teamId: number }> = {}

  for (const game of matchup.games) {
    for (const ps of [...game.homePlayerStats, ...game.visitorPlayerStats]) {
      if (!playerGames[ps.playerId]) {
        const teamId =
          game.homePlayerStats.includes(ps) ? game.homeTeam.id : game.visitorTeam.id
        playerGames[ps.playerId] = { stats: [], teamId }
      }
      playerGames[ps.playerId].stats.push(ps)
    }
  }

  const outperformers: OutperformingPlayer[] = []

  for (const [playerIdStr, { stats, teamId }] of Object.entries(playerGames)) {
    const playerId = Number(playerIdStr)
    const sa = seasonAverages[playerId]
    if (!sa || stats.length < 2) continue

    const count = stats.length
    const vsAvg = {
      ptsAvg: stats.reduce((s, g) => s + g.points, 0) / count,
      rebAvg: stats.reduce((s, g) => s + g.rebounds, 0) / count,
      astAvg: stats.reduce((s, g) => s + g.assists, 0) / count,
      fgPct: stats.reduce((s, g) => s + g.fieldGoalPct, 0) / count,
      gamesPlayed: count,
    }

    const delta = {
      pts: vsAvg.ptsAvg - sa.ptsAvg,
      reb: vsAvg.rebAvg - sa.rebAvg,
      ast: vsAvg.astAvg - sa.astAvg,
      fgPct: vsAvg.fgPct - sa.fgPct,
    }

    // Include if points are +3 higher or any counting stat is +2 higher
    const significant =
      delta.pts >= 3 || delta.reb >= 2 || delta.ast >= 2 || delta.fgPct >= 0.03

    if (significant) {
      outperformers.push({
        playerId,
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
  const outperformers = useMemo(
    () => computeOutperformers(matchup, seasonAverages),
    [matchup, seasonAverages]
  )

  if (outperformers.length === 0) {
    return (
      <div className="py-8 text-center text-sm text-muted-foreground">
        No significant outperformers found in the last {matchup.games.length} matchups.
      </div>
    )
  }

  return (
    <div>
      <p className="mb-4 text-sm text-muted-foreground">
        Players who significantly outperform their season averages against this opponent.
      </p>
      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
        {outperformers.map((player) => (
          <PlayerOutperformCard
            key={player.playerId}
            player={player}
            teamColor={getTeamColors(player.teamId).primary}
          />
        ))}
      </div>
    </div>
  )
}
