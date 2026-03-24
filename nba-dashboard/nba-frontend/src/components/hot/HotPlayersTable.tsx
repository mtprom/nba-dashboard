import {
  Table,
  TableHeader,
  TableBody,
  TableHead,
  TableRow,
  TableCell,
} from "@/components/ui/table"
import HeatBadge from "./HeatBadge"
import DeltaStat from "./DeltaStat"
import { getTeamColors } from "@/data/teams"
import type { HotPlayer } from "@/types"

interface HotPlayersTableProps {
  players: HotPlayer[]
}

export default function HotPlayersTable({ players }: HotPlayersTableProps) {
  if (players.length === 0) {
    return (
      <div className="py-12 text-center text-sm text-muted-foreground">
        No player data available yet. Check back once game data has been synced.
      </div>
    )
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead className="w-10 text-center">#</TableHead>
          <TableHead>Player</TableHead>
          <TableHead className="text-center">Heat</TableHead>
          <TableHead className="text-center">PTS</TableHead>
          <TableHead className="text-center">REB</TableHead>
          <TableHead className="text-center">AST</TableHead>
          <TableHead className="text-center hidden sm:table-cell">FG%</TableHead>
          <TableHead className="text-center hidden md:table-cell">TS%</TableHead>
          <TableHead className="text-center hidden lg:table-cell">NetRtg</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {players.map((player, i) => {
          const colors = getTeamColors(player.team.id)
          return (
            <TableRow key={player.playerId}>
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
                    <div className="font-medium">{player.playerName}</div>
                    <div className="text-xs text-muted-foreground">
                      {player.position} &middot; {player.team.abbreviation}
                    </div>
                  </div>
                </div>
              </TableCell>
              <TableCell className="text-center">
                <HeatBadge score={player.heatScore} />
              </TableCell>
              <TableCell>
                <DeltaStat
                  value={player.ptsAvg}
                  baseline={player.baselinePtsAvg}
                  delta={player.ptsDelta}
                  label="PTS"
                />
              </TableCell>
              <TableCell>
                <DeltaStat
                  value={player.rebAvg}
                  baseline={player.baselineRebAvg}
                  delta={player.rebDelta}
                  label="REB"
                />
              </TableCell>
              <TableCell>
                <DeltaStat
                  value={player.astAvg}
                  baseline={player.baselineAstAvg}
                  delta={player.astDelta}
                  label="AST"
                />
              </TableCell>
              <TableCell className="hidden sm:table-cell">
                <DeltaStat
                  value={player.fgPct}
                  baseline={player.baselineFgPct}
                  delta={player.fgPctDelta}
                  label="FG%"
                  isPct
                />
              </TableCell>
              <TableCell className="hidden md:table-cell">
                <DeltaStat
                  value={player.tsPct}
                  baseline={player.baselineTsPct}
                  delta={player.tsPctDelta}
                  label="TS%"
                  isPct
                />
              </TableCell>
              <TableCell className="hidden lg:table-cell">
                <DeltaStat
                  value={player.netRating}
                  baseline={player.baselineNetRating}
                  delta={player.netRatingDelta}
                  label="NetRtg"
                />
              </TableCell>
            </TableRow>
          )
        })}
      </TableBody>
    </Table>
  )
}
