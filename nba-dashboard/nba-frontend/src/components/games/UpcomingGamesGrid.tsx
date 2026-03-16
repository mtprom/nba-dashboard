import GameCard from "./GameCard"
import GameCardSkeleton from "./GameCardSkeleton"
import type { UpcomingGame } from "@/types"

interface UpcomingGamesGridProps {
  games: UpcomingGame[]
  selectedGameId: string | null
  onSelectGame: (gameId: string) => void
  loading: boolean
}

export default function UpcomingGamesGrid({
  games,
  selectedGameId,
  onSelectGame,
  loading,
}: UpcomingGamesGridProps) {
  if (loading) {
    return (
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {Array.from({ length: 6 }).map((_, i) => (
          <GameCardSkeleton key={i} />
        ))}
      </div>
    )
  }

  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {games.map((game) => (
        <GameCard
          key={game.game.id}
          game={game}
          isSelected={selectedGameId === game.game.id}
          onSelect={onSelectGame}
        />
      ))}
    </div>
  )
}
