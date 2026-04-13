import { useMemo } from "react"
import PlayerOutperformCard from "./PlayerOutperformCard"
import { getTeamColors } from "@/data/teams"
import type { MatchupHistory, MatchupGame, PlayerSeasonAvg, OutperformingPlayer } from "@/types"

interface PlayersToWatchProps {
  matchup: MatchupHistory
  seasonAverages: Record<number, PlayerSeasonAvg>
  seasonAveragesError: boolean
}

function getCurrentSeasonYear(): number {
  const now = new Date()
  return now.getMonth() >= 9 ? now.getFullYear() : now.getFullYear() - 1
}

function getRelevantMatchupGame(matchup: MatchupHistory): {
  game: MatchupGame | null
  usesFallback: boolean
} {
  const seasonYear = getCurrentSeasonYear()
  const seasonStart = `${seasonYear}-10-01`
  const currentSeasonGame = matchup.games.find((mg) => mg.game.date >= seasonStart)

  if (currentSeasonGame) {
    return { game: currentSeasonGame, usesFallback: false }
  }

  return { game: matchup.games[0] ?? null, usesFallback: matchup.games.length > 0 }
}

function formatGameDate(gameDate: string): string {
  return new Date(gameDate).toLocaleDateString("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric",
    timeZone: "UTC",
  })
}

function computeOutperformers(
  game: MatchupGame,
  seasonAverages: Record<number, PlayerSeasonAvg>
): { comparablePlayers: number; outperformers: OutperformingPlayer[] } {
  const allStats = [
    ...game.homePlayerStats.map((ps) => ({ ps, teamId: game.homeTeam.id })),
    ...game.visitorPlayerStats.map((ps) => ({ ps, teamId: game.visitorTeam.id })),
  ]

  const outperformers: OutperformingPlayer[] = []
  let comparablePlayers = 0

  for (const { ps, teamId } of allStats) {
    const sa = seasonAverages[ps.playerId]
    if (!sa) continue
    comparablePlayers += 1

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
        jerseyNumber: ps.jerseyNumber || sa.jerseyNumber,
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

  return {
    comparablePlayers,
    outperformers: outperformers.sort((a, b) => b.delta.pts - a.delta.pts),
  }
}

function FallbackNote({ gameDate }: { gameDate: string }) {
  return (
    <p className="mb-4 text-sm text-muted-foreground">
      Using the most recent head-to-head from {formatGameDate(gameDate)} because these teams have
      not met this season yet.
    </p>
  )
}

export default function PlayersToWatch({
  matchup,
  seasonAverages,
  seasonAveragesError,
}: PlayersToWatchProps) {
  const selectedMatchup = useMemo(() => getRelevantMatchupGame(matchup), [matchup])

  const comparison = useMemo(
    () =>
      selectedMatchup.game
        ? computeOutperformers(selectedMatchup.game, seasonAverages)
        : { comparablePlayers: 0, outperformers: [] },
    [selectedMatchup, seasonAverages]
  )

  if (matchup.games.length === 0) {
    return (
      <div className="py-8 text-center text-sm text-muted-foreground">
        No matchup history is available for these teams yet.
      </div>
    )
  }

  if (seasonAveragesError) {
    return (
      <div className="py-8 text-center text-sm text-muted-foreground">
        Current season averages are unavailable right now, so players to watch cannot be computed.
      </div>
    )
  }

  if (!selectedMatchup.game) {
    return (
      <div className="py-8 text-center text-sm text-muted-foreground">
        No usable matchup data is available for players to watch.
      </div>
    )
  }

  if (comparison.comparablePlayers === 0) {
    return (
      <div className="py-8 text-center text-sm text-muted-foreground">
        {selectedMatchup.usesFallback && <FallbackNote gameDate={selectedMatchup.game.game.date} />}
        Current season averages are not available for players from the latest matchup.
      </div>
    )
  }

  if (comparison.outperformers.length === 0) {
    return (
      <div className="py-8 text-center text-sm text-muted-foreground">
        {selectedMatchup.usesFallback && <FallbackNote gameDate={selectedMatchup.game.game.date} />}
        No standouts in the latest matchup with available season averages.
      </div>
    )
  }

  return (
    <div>
      {selectedMatchup.usesFallback && <FallbackNote gameDate={selectedMatchup.game.game.date} />}
      <p className="mb-4 text-sm text-muted-foreground">
        Players who significantly outperformed their season averages in the last matchup.
      </p>
      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
        {comparison.outperformers.map((player) => (
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
