import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card"
import { BestWorstGames, HistoryGame } from "@/types/history"
import { TEAM_INFO } from "@/data/teams"
import { formatDate } from "./GameLogPanel"

interface BestWorstGamesPanelProps {
  data: BestWorstGames
  teamColor: string
}

interface GameCardProps {
  label: string
  game: HistoryGame | null
  statLabel: string
  statValue: (g: HistoryGame) => string
  teamColor: string
}

function GameCard({ label, game, statLabel, statValue, teamColor }: GameCardProps) {
  const empty = !game

  return (
    <div className="rounded-lg border border-border bg-card p-4 flex flex-col gap-2">
      <div
        className="text-[10px] font-semibold uppercase tracking-wider"
        style={{ color: teamColor }}
      >
        {label}
      </div>
      {empty ? (
        <div className="text-sm text-muted-foreground">—</div>
      ) : (
        <>
          <div className="text-xs text-muted-foreground">{formatDate(game.date)}</div>
          <div className="flex items-center gap-1.5 text-sm font-medium">
            <span>{TEAM_INFO[game.homeTeamId]?.abbreviation ?? "???"}</span>
            <span className="font-mono text-foreground">
              {game.homeScore}–{game.awayScore}
            </span>
            <span>{TEAM_INFO[game.awayTeamId]?.abbreviation ?? "???"}</span>
          </div>
          <div className="text-xs text-muted-foreground">
            {statLabel}:{" "}
            <span className="font-semibold text-foreground">{statValue(game)}</span>
          </div>
        </>
      )}
    </div>
  )
}

export default function BestWorstGamesPanel({ data, teamColor }: BestWorstGamesPanelProps) {
  const margin = (g: HistoryGame) => `${Math.abs(g.homeScore - g.awayScore)} pts`
  const total = (g: HistoryGame) => `${g.homeScore + g.awayScore} pts combined`

  return (
    <Card>
      <CardHeader className="pb-2">
        <CardTitle className="text-base font-medium">Best &amp; Worst Games</CardTitle>
      </CardHeader>
      <CardContent>
        <div className="grid grid-cols-2 gap-3">
          <GameCard
            label="Largest Win"
            game={data.largestWin}
            statLabel="Margin"
            statValue={margin}
            teamColor={teamColor}
          />
          <GameCard
            label="Largest Loss"
            game={data.largestLoss}
            statLabel="Margin"
            statValue={margin}
            teamColor={teamColor}
          />
          <GameCard
            label="Highest Scoring"
            game={data.highestScoringGame}
            statLabel="Total"
            statValue={total}
            teamColor={teamColor}
          />
          <GameCard
            label="Lowest Scoring"
            game={data.lowestScoringGame}
            statLabel="Total"
            statValue={total}
            teamColor={teamColor}
          />
        </div>
      </CardContent>
    </Card>
  )
}
