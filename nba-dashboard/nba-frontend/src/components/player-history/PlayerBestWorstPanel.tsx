import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { formatPlayerHistoryDate } from "@/data/player-history"
import type {
  PlayerHistoryHighlightGame,
  PlayerHistoryHighlights,
} from "@/types/player-history"
import { TEAM_INFO } from "@/data/teams"

interface PlayerBestWorstPanelProps {
  highlights: PlayerHistoryHighlights
  accentColor: string
}

interface HighlightDef {
  key: keyof PlayerHistoryHighlights
  label: string
  statLabel: string
  value: (game: PlayerHistoryHighlightGame) => string
}

const highlightDefs: HighlightDef[] = [
  { key: "highestPoints", label: "Highest Points", statLabel: "PTS", value: (g) => `${g.points}` },
  { key: "highestRebounds", label: "Highest Rebounds", statLabel: "REB", value: (g) => `${g.rebounds}` },
  { key: "highestAssists", label: "Highest Assists", statLabel: "AST", value: (g) => `${g.assists}` },
  { key: "bestEfficiency", label: "Best Efficiency", statLabel: "TS%", value: (g) => g.tsPct != null ? `${(g.tsPct * 100).toFixed(1)}%` : "—" },
  { key: "worstShooting", label: "Worst Shooting", statLabel: "FG", value: (g) => `${g.fieldGoalsMade}-${g.fieldGoalsAttempted} (${(g.fieldGoalPct * 100).toFixed(1)}%)` },
  { key: "bestPlusMinus", label: "Best Plus/Minus", statLabel: "+/-", value: (g) => `${g.plusMinus > 0 ? "+" : ""}${g.plusMinus}` },
  { key: "worstPlusMinus", label: "Worst Plus/Minus", statLabel: "+/-", value: (g) => `${g.plusMinus > 0 ? "+" : ""}${g.plusMinus}` },
]

export default function PlayerBestWorstPanel({
  highlights,
  accentColor,
}: PlayerBestWorstPanelProps) {
  return (
    <Card>
      <CardHeader className="pb-2">
        <CardTitle className="text-base font-medium">Best &amp; Worst Performances</CardTitle>
      </CardHeader>
      <CardContent>
        <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
          {highlightDefs.map(({ key, label, statLabel, value }) => {
            const game = highlights[key]
            return (
              <div key={key} className="rounded-lg border border-border bg-card p-4">
                <div
                  className="text-[10px] font-semibold uppercase tracking-wider"
                  style={{ color: accentColor }}
                >
                  {label}
                </div>
                {!game ? (
                  <div className="mt-2 text-sm text-muted-foreground">No qualifying game</div>
                ) : (
                  <div className="mt-2 space-y-1.5">
                    <div className="text-xs text-muted-foreground">
                      {formatPlayerHistoryDate(game.date)} · {game.isHome ? "vs" : "@"} {TEAM_INFO[game.opponentTeamId]?.abbreviation ?? "???"}
                    </div>
                    <div className="text-sm font-medium">
                      {TEAM_INFO[game.homeTeamId]?.abbreviation ?? "???"} {game.homeScore} - {game.awayScore} {TEAM_INFO[game.awayTeamId]?.abbreviation ?? "???"}
                    </div>
                    <div className="text-sm text-foreground">
                      <span className="text-muted-foreground">{statLabel}:</span> {value(game)}
                    </div>
                  </div>
                )}
              </div>
            )
          })}
        </div>
      </CardContent>
    </Card>
  )
}
