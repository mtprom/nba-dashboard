import { useState, useEffect } from "react"
import Header from "@/components/layout/Header"
import PageContainer from "@/components/layout/PageContainer"
import UpcomingGamesGrid from "@/components/games/UpcomingGamesGrid"
import MatchupPanel from "@/components/matchup/MatchupPanel"
import { getUpcomingGames, getMatchupHistory, getSeasonAverages } from "@/api/games"
import type { UpcomingGame, MatchupHistory, PlayerSeasonAvg } from "@/types"

export default function GamePreviewPage() {
  const [games, setGames] = useState<UpcomingGame[]>([])
  const [loading, setLoading] = useState(true)
  const [selectedGameId, setSelectedGameId] = useState<string | null>(null)
  const [matchup, setMatchup] = useState<MatchupHistory | null>(null)
  const [seasonAverages, setSeasonAverages] = useState<Record<number, PlayerSeasonAvg>>({})
  const [matchupLoading, setMatchupLoading] = useState(false)

  useEffect(() => {
    async function load() {
      const [gamesData, averages] = await Promise.all([
        getUpcomingGames(),
        getSeasonAverages(),
      ])
      setGames(gamesData)
      setSeasonAverages(averages)
      setLoading(false)
    }
    load()
  }, [])

  useEffect(() => {
    if (!selectedGameId) {
      setMatchup(null)
      return
    }

    const game = games.find((g) => g.game.id === selectedGameId)
    if (!game) return

    setMatchupLoading(true)
    getMatchupHistory(game.game.homeTeamId, game.game.visitorTeamId).then((data) => {
      setMatchup(data)
      setMatchupLoading(false)
    })
  }, [selectedGameId, games])

  const handleSelectGame = (gameId: string) => {
    setSelectedGameId((prev) => (prev === gameId ? null : gameId))
  }

  return (
    <div>
      <Header />
      <PageContainer>
        <div className="mb-6">
          <h2 className="text-2xl font-bold">Today's Games</h2>
          <p className="text-sm text-muted-foreground">
            Select a game to view matchup history and players to watch
          </p>
        </div>

        <UpcomingGamesGrid
          games={games}
          selectedGameId={selectedGameId}
          onSelectGame={handleSelectGame}
          loading={loading}
        />

        {matchupLoading && (
          <div className="mt-6 text-center text-sm text-muted-foreground">
            Loading matchup data...
          </div>
        )}

        {matchup && !matchupLoading && (
          <MatchupPanel matchup={matchup} seasonAverages={seasonAverages} />
        )}
      </PageContainer>
    </div>
  )
}
