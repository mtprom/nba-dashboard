import { Card } from "@/components/ui/card"
import StatComparisonBar from "./StatComparisonBar"
import type { OutperformingPlayer } from "@/types"

interface PlayerOutperformCardProps {
  player: OutperformingPlayer
  teamColor: string
}

export default function PlayerOutperformCard({ player, teamColor }: PlayerOutperformCardProps) {
  return (
    <Card>
      <div className="p-4">
        {/* Player header */}
        <div className="mb-3 flex items-center gap-3">
          <div
            className="flex h-10 w-10 items-center justify-center rounded-full text-sm font-bold"
            style={{ backgroundColor: teamColor, color: "#fff" }}
          >
            {player.jerseyNumber
              ? `#${player.jerseyNumber}`
              : player.playerName.split(" ").map(n => n[0]).join("")}
          </div>
          <div>
            <div className="text-sm font-semibold">{player.playerName}</div>
            <div className="text-xs text-muted-foreground">
              {player.position} &middot; Last matchup
            </div>
          </div>
        </div>

        {/* Stat comparisons */}
        <div className="space-y-3">
          <StatComparisonBar
            label="PTS"
            seasonAvg={player.seasonAvg.ptsAvg}
            vsOpponentAvg={player.vsOpponent.ptsAvg}
            teamColor={teamColor}
          />
          <StatComparisonBar
            label="REB"
            seasonAvg={player.seasonAvg.rebAvg}
            vsOpponentAvg={player.vsOpponent.rebAvg}
            teamColor={teamColor}
          />
          <StatComparisonBar
            label="AST"
            seasonAvg={player.seasonAvg.astAvg}
            vsOpponentAvg={player.vsOpponent.astAvg}
            teamColor={teamColor}
          />
          <StatComparisonBar
            label="FG%"
            seasonAvg={player.seasonAvg.fgPct}
            vsOpponentAvg={player.vsOpponent.fgPct}
            teamColor={teamColor}
            format="pct"
          />
        </div>
      </div>
    </Card>
  )
}
