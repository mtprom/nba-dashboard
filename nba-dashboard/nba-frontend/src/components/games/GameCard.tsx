import { useRef } from "react"
import { Card } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { useTeamColors } from "@/hooks/useTeamColors"
import { getTeamColors, TEAM_INFO } from "@/data/teams"
import { cn } from "@/lib/utils"
import type { UpcomingGame } from "@/types"

interface GameCardProps {
  game: UpcomingGame
  isSelected: boolean
  onSelect: (gameId: string) => void
}

export default function GameCard({ game, isSelected, onSelect }: GameCardProps) {
  const cardRef = useRef<HTMLDivElement>(null)
  useTeamColors(game.game.homeTeamId, game.game.visitorTeamId, cardRef)

  const homeColors = getTeamColors(game.game.homeTeamId)
  const awayColors = getTeamColors(game.game.visitorTeamId)

  const gameTime = new Date(game.game.date).toLocaleTimeString("en-US", {
    hour: "numeric",
    minute: "2-digit",
    timeZoneName: "short",
  })

  return (
    <Card
      ref={cardRef}
      onClick={() => onSelect(game.game.id)}
      className={cn(
        "cursor-pointer transition-all hover:scale-[1.02] hover:shadow-lg",
        isSelected && "ring-2 ring-ring"
      )}
    >
      <div className="p-4">
        {/* Team matchup */}
        <div className="flex items-center justify-between gap-3">
          {/* Away team */}
          <div className="flex flex-1 flex-col items-center gap-1">
            <div
              className="flex h-12 w-12 items-center justify-center rounded-full text-sm font-bold"
              style={{ backgroundColor: awayColors.primary, color: awayColors.secondary }}
            >
              {game.visitorTeam.abbreviation || TEAM_INFO[game.visitorTeam.id]?.abbreviation || "?"}
            </div>
            <span className="text-xs text-muted-foreground">{game.visitorTeam.city}</span>
            <span className="text-sm font-semibold">{game.visitorTeam.name}</span>
          </div>

          {/* VS / Score */}
          <div className="flex flex-col items-center gap-1">
            {game.game.status === "Scheduled" ? (
              <>
                <span className="text-xs font-medium text-muted-foreground">@</span>
              </>
            ) : (
              <div className="flex gap-2 text-lg font-bold">
                <span>{game.game.visitorScore}</span>
                <span className="text-muted-foreground">-</span>
                <span>{game.game.homeScore}</span>
              </div>
            )}
            <Badge variant="secondary" className="text-[10px]">
              {game.game.status === "Scheduled" ? gameTime : game.game.status}
            </Badge>
          </div>

          {/* Home team */}
          <div className="flex flex-1 flex-col items-center gap-1">
            <div
              className="flex h-12 w-12 items-center justify-center rounded-full text-sm font-bold"
              style={{ backgroundColor: homeColors.primary, color: homeColors.secondary }}
            >
              {game.homeTeam.abbreviation || TEAM_INFO[game.homeTeam.id]?.abbreviation || "?"}
            </div>
            <span className="text-xs text-muted-foreground">{game.homeTeam.city}</span>
            <span className="text-sm font-semibold">{game.homeTeam.name}</span>
          </div>
        </div>

        {/* Arena */}
        <div className="mt-3 text-center text-xs text-muted-foreground">
          {game.game.arena}
        </div>
      </div>
    </Card>
  )
}
