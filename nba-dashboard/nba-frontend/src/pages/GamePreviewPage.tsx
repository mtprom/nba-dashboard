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
  const [seasonAveragesError, setSeasonAveragesError] = useState(false)
  const [matchupLoading, setMatchupLoading] = useState(false)
  const [matchupError, setMatchupError] = useState<string | null>(null)

  useEffect(() => {
    async function load() {
      try {
        const gamesData = await getUpcomingGames()
        setGames(gamesData)

        const teamIds = [
          ...new Set(
            gamesData.flatMap((g) => [g.game.homeTeamId, g.game.visitorTeamId])
          ),
        ]
        try {
          const averages =
            teamIds.length > 0 ? await getSeasonAverages(teamIds) : {}
          setSeasonAverages(averages)
          setSeasonAveragesError(false)
        } catch {
          console.warn("Failed to load season averages")
          setSeasonAverages({})
          setSeasonAveragesError(true)
        }
      } catch (err) {
        console.error("Failed to load games", err)
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [])

  useEffect(() => {
    if (!selectedGameId) {
      setMatchup(null)
      setMatchupError(null)
      return
    }

    const game = games.find((g) => g.game.id === selectedGameId)
    if (!game) {
      setMatchup(null)
      setMatchupError("Could not find that game.")
      return
    }

    let cancelled = false

    async function loadMatchup() {
      setMatchup(null)
      setMatchupError(null)
      setMatchupLoading(true)

      try {
        const data = await getMatchupHistory(game.game.homeTeamId, game.game.visitorTeamId)
        if (!cancelled) {
          setMatchup(data)
        }
      } catch (err) {
        console.error("Failed to load matchup history", err)
        if (!cancelled) {
          setMatchup(null)
          setMatchupError("Failed to load matchup history for this game.")
        }
      } finally {
        if (!cancelled) {
          setMatchupLoading(false)
        }
      }
    }

    loadMatchup()

    return () => {
      cancelled = true
    }
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

        {matchupError && !matchupLoading && (
          <div className="mt-6 rounded-lg border border-border bg-card p-4 text-sm text-muted-foreground">
            {matchupError}
          </div>
        )}

        {matchup && !matchupLoading && (
          <MatchupPanel
            matchup={matchup}
            seasonAverages={seasonAverages}
            seasonAveragesError={seasonAveragesError}
          />
        )}
      </PageContainer>
    </div>
  )
}
