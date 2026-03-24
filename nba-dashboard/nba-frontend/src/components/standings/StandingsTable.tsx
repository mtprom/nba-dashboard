import {
  Table,
  TableHeader,
  TableBody,
  TableHead,
  TableRow,
  TableCell,
} from "@/components/ui/table"
import { getTeamColors } from "@/data/teams"
import type { StandingsEntry } from "@/types"

interface StandingsTableProps {
  standings: StandingsEntry[]
}

export default function StandingsTable({ standings }: StandingsTableProps) {
  if (standings.length === 0) {
    return (
      <div className="py-12 text-center text-sm text-muted-foreground">
        No standings data available.
      </div>
    )
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead className="w-12 text-center">#</TableHead>
          <TableHead>Team</TableHead>
          <TableHead className="text-center">W</TableHead>
          <TableHead className="text-center">L</TableHead>
          <TableHead className="text-center">PCT</TableHead>
          <TableHead className="text-center hidden sm:table-cell">Home</TableHead>
          <TableHead className="text-center hidden sm:table-cell">Away</TableHead>
          <TableHead className="text-center">L10</TableHead>
          <TableHead className="text-center">Streak</TableHead>
          <TableHead className="text-center hidden lg:table-cell">OffRtg</TableHead>
          <TableHead className="text-center hidden lg:table-cell">DefRtg</TableHead>
          <TableHead className="text-center hidden md:table-cell">NetRtg</TableHead>
          <TableHead className="text-center hidden lg:table-cell">Pace</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {standings.map((entry) => {
          const colors = getTeamColors(entry.team.id)
          const netRating = entry.netRating
          const isWinStreak = entry.streak.startsWith("W")

          return (
            <TableRow
              key={entry.team.id}
              className={entry.confRank > 10 ? "opacity-60" : ""}
            >
              <TableCell className="text-center font-medium text-muted-foreground">
                {entry.confRank}
              </TableCell>
              <TableCell>
                <div className="flex items-center gap-2">
                  <div
                    className="h-3 w-3 rounded-full shrink-0"
                    style={{ backgroundColor: colors.primary }}
                  />
                  <span className="font-medium hidden sm:inline">
                    {entry.team.city}{" "}
                  </span>
                  <span className="font-medium">{entry.team.name}</span>
                </div>
              </TableCell>
              <TableCell className="text-center font-medium">{entry.wins}</TableCell>
              <TableCell className="text-center font-medium">{entry.losses}</TableCell>
              <TableCell className="text-center">
                {entry.winPct.toFixed(3).replace(/^0/, "")}
              </TableCell>
              <TableCell className="text-center hidden sm:table-cell">
                {entry.homeRecord}
              </TableCell>
              <TableCell className="text-center hidden sm:table-cell">
                {entry.awayRecord}
              </TableCell>
              <TableCell className="text-center">{entry.last10}</TableCell>
              <TableCell
                className={`text-center font-medium ${
                  isWinStreak ? "text-green-500" : "text-red-500"
                }`}
              >
                {entry.streak}
              </TableCell>
              <TableCell className="text-center hidden lg:table-cell">
                {entry.offRating.toFixed(1)}
              </TableCell>
              <TableCell className="text-center hidden lg:table-cell">
                {entry.defRating.toFixed(1)}
              </TableCell>
              <TableCell
                className={`text-center hidden md:table-cell font-medium ${
                  netRating > 0 ? "text-green-500" : netRating < 0 ? "text-red-500" : ""
                }`}
              >
                {netRating > 0 ? "+" : ""}
                {netRating.toFixed(1)}
              </TableCell>
              <TableCell className="text-center hidden lg:table-cell">
                {entry.pace.toFixed(1)}
              </TableCell>
            </TableRow>
          )
        })}
      </TableBody>
    </Table>
  )
}
