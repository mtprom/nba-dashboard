import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { getTeamColors } from "@/data/teams"
import type { MatchupGame } from "@/types"

interface MatchupHistoryTableProps {
  games: MatchupGame[]
}

export default function MatchupHistoryTable({ games }: MatchupHistoryTableProps) {
  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Date</TableHead>
          <TableHead>Matchup</TableHead>
          <TableHead className="text-right">Score</TableHead>
          <TableHead className="text-right">Winner</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {games.map((mg) => {
          const homeWon = mg.game.homeScore > mg.game.visitorScore
          const winner = homeWon ? mg.homeTeam : mg.visitorTeam
          const winnerColors = getTeamColors(winner.id)

          const dateStr = new Date(mg.game.date).toLocaleDateString("en-US", {
            month: "short",
            day: "numeric",
            year: "numeric",
          })

          return (
            <TableRow key={mg.game.id}>
              <TableCell className="text-muted-foreground">{dateStr}</TableCell>
              <TableCell>
                <span className="font-medium">{mg.visitorTeam.abbreviation}</span>
                <span className="mx-1 text-muted-foreground">@</span>
                <span className="font-medium">{mg.homeTeam.abbreviation}</span>
              </TableCell>
              <TableCell className="text-right font-mono">
                {mg.game.visitorScore} - {mg.game.homeScore}
              </TableCell>
              <TableCell className="text-right">
                <span
                  className="font-semibold"
                  style={{ color: winnerColors.primary }}
                >
                  {winner.abbreviation}
                </span>
              </TableCell>
            </TableRow>
          )
        })}
      </TableBody>
    </Table>
  )
}
