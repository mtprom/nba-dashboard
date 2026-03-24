import {
  Table,
  TableHeader,
  TableBody,
  TableHead,
  TableRow,
  TableCell,
} from "@/components/ui/table"
import HeatBadge from "./HeatBadge"
import { getTeamColors } from "@/data/teams"
import type { HotTeam } from "@/types"

interface HotTeamsTableProps {
  teams: HotTeam[]
}

export default function HotTeamsTable({ teams }: HotTeamsTableProps) {
  if (teams.length === 0) {
    return (
      <div className="py-12 text-center text-sm text-muted-foreground">
        No team data available yet. Check back once game data has been synced.
      </div>
    )
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead className="w-10 text-center">#</TableHead>
          <TableHead>Team</TableHead>
          <TableHead className="text-center">Heat</TableHead>
          <TableHead className="text-center">Record</TableHead>
          <TableHead className="text-center">Win%</TableHead>
          <TableHead className="text-center hidden sm:table-cell">PPG</TableHead>
          <TableHead className="text-center hidden sm:table-cell">Opp PPG</TableHead>
          <TableHead className="text-center hidden md:table-cell">NetRtg</TableHead>
          <TableHead className="text-center hidden lg:table-cell">vs Baseline</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {teams.map((entry, i) => {
          const colors = getTeamColors(entry.team.id)
          const netDeltaColor =
            entry.netRatingDelta > 0
              ? "text-green-500"
              : entry.netRatingDelta < 0
                ? "text-red-500"
                : ""

          return (
            <TableRow key={entry.team.id}>
              <TableCell className="text-center text-muted-foreground font-medium">
                {i + 1}
              </TableCell>
              <TableCell>
                <div className="flex items-center gap-2">
                  <div
                    className="h-3 w-3 rounded-full shrink-0"
                    style={{ backgroundColor: colors.primary }}
                  />
                  <div>
                    <span className="font-medium hidden sm:inline">
                      {entry.team.city}{" "}
                    </span>
                    <span className="font-medium">{entry.team.name}</span>
                  </div>
                </div>
              </TableCell>
              <TableCell className="text-center">
                <HeatBadge score={entry.heatScore} />
              </TableCell>
              <TableCell className="text-center font-medium">
                {entry.windowWins}-{entry.windowLosses}
              </TableCell>
              <TableCell className="text-center">
                <div>{entry.windowWinPct.toFixed(3).replace(/^0/, "")}</div>
                <div
                  className={`text-xs font-medium ${
                    entry.winPctDelta > 0
                      ? "text-green-500"
                      : entry.winPctDelta < 0
                        ? "text-red-500"
                        : "text-muted-foreground"
                  }`}
                >
                  {entry.winPctDelta > 0 ? "+" : ""}
                  {entry.winPctDelta.toFixed(3)}
                </div>
              </TableCell>
              <TableCell className="text-center hidden sm:table-cell">
                <div>{entry.windowPtsScored.toFixed(1)}</div>
                <div
                  className={`text-xs font-medium ${
                    entry.scoringDelta > 0 ? "text-green-500" : entry.scoringDelta < 0 ? "text-red-500" : ""
                  }`}
                >
                  {entry.scoringDelta > 0 ? "+" : ""}
                  {entry.scoringDelta.toFixed(1)}
                </div>
              </TableCell>
              <TableCell className="text-center hidden sm:table-cell">
                {entry.windowPtsAllowed.toFixed(1)}
              </TableCell>
              <TableCell className={`text-center hidden md:table-cell font-medium ${netDeltaColor}`}>
                {entry.windowNetRating > 0 ? "+" : ""}
                {entry.windowNetRating.toFixed(1)}
              </TableCell>
              <TableCell className="text-center hidden lg:table-cell">
                <div className="text-xs text-muted-foreground">
                  Season: {entry.baselineNetRating > 0 ? "+" : ""}
                  {entry.baselineNetRating.toFixed(1)}
                </div>
                <div
                  className={`text-xs font-medium ${netDeltaColor}`}
                >
                  {entry.netRatingDelta > 0 ? "\u2191+" : "\u2193"}
                  {entry.netRatingDelta.toFixed(1)}
                </div>
              </TableCell>
            </TableRow>
          )
        })}
      </TableBody>
    </Table>
  )
}
