import { Card, CardContent } from "@/components/ui/card"
import type { PlayerHistoryPlayer } from "@/types/player-history"

interface PlayerHeaderCardProps {
  player: PlayerHistoryPlayer
  accentColor: string
}

export default function PlayerHeaderCard({ player, accentColor }: PlayerHeaderCardProps) {
  return (
    <Card className="overflow-hidden">
      <div className="h-1.5 w-full" style={{ backgroundColor: accentColor }} />
      <CardContent className="flex flex-col gap-4 pt-5 md:flex-row md:items-end md:justify-between">
        <div className="space-y-2">
          <div className="flex flex-wrap items-center gap-2">
            <h3 className="text-2xl font-semibold tracking-tight">{player.playerName}</h3>
            <span className="rounded-full border border-border px-2 py-0.5 text-xs text-muted-foreground">
              {player.position || "—"} {player.jerseyNumber ? `#${player.jerseyNumber}` : ""}
            </span>
            <span
              className="rounded-full px-2 py-0.5 text-xs font-medium"
              style={{
                backgroundColor: player.isActive ? "rgba(34,197,94,0.14)" : "rgba(148,163,184,0.14)",
                color: player.isActive ? "rgb(34,197,94)" : "rgb(148,163,184)",
              }}
            >
              {player.isActive ? "Active" : "Inactive"}
            </span>
          </div>
          <p className="text-sm text-muted-foreground">
            {player.currentTeamName || "No current team"} · Career span {player.firstSeasonYear}-{String(player.lastSeasonYear + 1).slice(2)}
          </p>
        </div>
        <div className="flex flex-wrap gap-3 text-sm">
          <div className="rounded-md border border-border bg-muted/40 px-3 py-2">
            <div className="text-xs text-muted-foreground">Current Team</div>
            <div className="font-medium">{player.currentTeamAbbreviation || "FA"}</div>
          </div>
          <div className="rounded-md border border-border bg-muted/40 px-3 py-2">
            <div className="text-xs text-muted-foreground">First Season</div>
            <div className="font-medium">{player.firstSeasonYear}-{String(player.firstSeasonYear + 1).slice(2)}</div>
          </div>
          <div className="rounded-md border border-border bg-muted/40 px-3 py-2">
            <div className="text-xs text-muted-foreground">Latest Season</div>
            <div className="font-medium">{player.lastSeasonYear}-{String(player.lastSeasonYear + 1).slice(2)}</div>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
