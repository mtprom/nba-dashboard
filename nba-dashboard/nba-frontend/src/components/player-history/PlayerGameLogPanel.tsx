import { useState } from "react"
import {
  ChevronDown,
  ChevronUp,
  ChevronsUpDown,
} from "lucide-react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { ScrollArea } from "@/components/ui/scroll-area"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { formatPlayerHistoryDate } from "@/data/player-history"
import type { PlayerHistoryGame } from "@/types/player-history"
import { TEAM_INFO } from "@/data/teams"

type SortField = "date" | "points" | "rebounds" | "assists" | "minutes" | "plusMinus"
type SortDirection = "asc" | "desc"

interface PlayerGameLogPanelProps {
  games: PlayerHistoryGame[]
}

function SortIcon({
  field,
  activeField,
  direction,
}: {
  field: SortField
  activeField: SortField
  direction: SortDirection
}) {
  if (field !== activeField) return <ChevronsUpDown className="ml-0.5 inline h-3 w-3 opacity-40" />
  return direction === "asc"
    ? <ChevronUp className="ml-0.5 inline h-3 w-3" />
    : <ChevronDown className="ml-0.5 inline h-3 w-3" />
}

export default function PlayerGameLogPanel({ games }: PlayerGameLogPanelProps) {
  const [sortField, setSortField] = useState<SortField>("date")
  const [sortDirection, setSortDirection] = useState<SortDirection>("desc")
  const [expandedGames, setExpandedGames] = useState<string[]>([])

  function handleSort(field: SortField) {
    if (field === sortField) {
      setSortDirection((current) => current === "asc" ? "desc" : "asc")
      return
    }

    setSortField(field)
    setSortDirection(field === "date" ? "desc" : "desc")
  }

  function toggleExpanded(gameId: string) {
    setExpandedGames((current) =>
      current.includes(gameId)
        ? current.filter((id) => id !== gameId)
        : [...current, gameId],
    )
  }

  const sortedGames = [...games].sort((a, b) => {
    let result = 0

    switch (sortField) {
      case "points":
        result = a.points - b.points
        break
      case "rebounds":
        result = a.rebounds - b.rebounds
        break
      case "assists":
        result = a.assists - b.assists
        break
      case "minutes":
        result = a.minutes - b.minutes
        break
      case "plusMinus":
        result = a.plusMinus - b.plusMinus
        break
      case "date":
      default:
        result = a.date.localeCompare(b.date)
        break
    }

    return sortDirection === "asc" ? result : -result
  })

  return (
    <Card className="flex flex-col">
      <CardHeader className="pb-2">
        <CardTitle className="text-base font-medium">Game Log</CardTitle>
      </CardHeader>
      <CardContent className="px-6 pb-6 pt-0">
        <ScrollArea className="h-[520px]">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-[40px]" />
                <TableHead className="cursor-pointer whitespace-nowrap" onClick={() => handleSort("date")}>
                  Date <SortIcon field="date" activeField={sortField} direction={sortDirection} />
                </TableHead>
                <TableHead>Opp</TableHead>
                <TableHead>Result</TableHead>
                <TableHead className="cursor-pointer text-right" onClick={() => handleSort("minutes")}>
                  MIN <SortIcon field="minutes" activeField={sortField} direction={sortDirection} />
                </TableHead>
                <TableHead className="cursor-pointer text-right" onClick={() => handleSort("points")}>
                  PTS <SortIcon field="points" activeField={sortField} direction={sortDirection} />
                </TableHead>
                <TableHead className="cursor-pointer text-right" onClick={() => handleSort("rebounds")}>
                  REB <SortIcon field="rebounds" activeField={sortField} direction={sortDirection} />
                </TableHead>
                <TableHead className="cursor-pointer text-right" onClick={() => handleSort("assists")}>
                  AST <SortIcon field="assists" activeField={sortField} direction={sortDirection} />
                </TableHead>
                <TableHead className="cursor-pointer text-right" onClick={() => handleSort("plusMinus")}>
                  +/- <SortIcon field="plusMinus" activeField={sortField} direction={sortDirection} />
                </TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {sortedGames.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={9} className="py-8 text-center text-sm text-muted-foreground">
                    No games match the selected range.
                  </TableCell>
                </TableRow>
              ) : (
                sortedGames.map((game) => {
                  const expanded = expandedGames.includes(game.gameId)
                  return (
                    <>
                      <TableRow key={game.gameId}>
                        <TableCell>
                          <button
                            type="button"
                            className="rounded p-1 text-muted-foreground hover:bg-muted hover:text-foreground"
                            onClick={() => toggleExpanded(game.gameId)}
                          >
                            {expanded ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
                          </button>
                        </TableCell>
                        <TableCell className="whitespace-nowrap text-xs text-muted-foreground">
                          {formatPlayerHistoryDate(game.date)}
                        </TableCell>
                        <TableCell className="text-sm">
                          {game.isHome ? "vs" : "@"} {TEAM_INFO[game.opponentTeamId]?.abbreviation ?? "???"}
                        </TableCell>
                        <TableCell className={`text-sm font-medium ${game.won ? "text-foreground" : "text-muted-foreground"}`}>
                          {game.won ? "W" : "L"} {game.homeScore}-{game.awayScore}
                        </TableCell>
                        <TableCell className="text-right font-mono text-xs">{game.minutes.toFixed(1)}</TableCell>
                        <TableCell className="text-right font-mono text-xs">{game.points}</TableCell>
                        <TableCell className="text-right font-mono text-xs">{game.rebounds}</TableCell>
                        <TableCell className="text-right font-mono text-xs">{game.assists}</TableCell>
                        <TableCell className="text-right font-mono text-xs">
                          {game.plusMinus > 0 ? "+" : ""}{game.plusMinus}
                        </TableCell>
                      </TableRow>
                      {expanded && (
                        <TableRow key={`${game.gameId}-expanded`}>
                          <TableCell colSpan={9} className="bg-muted/25">
                            <div className="grid gap-3 py-2 md:grid-cols-3">
                              <div className="space-y-1 text-xs">
                                <div className="font-semibold text-foreground">Traditional</div>
                                <div className="text-muted-foreground">
                                  FG: <span className="text-foreground">{game.fieldGoalsMade}-{game.fieldGoalsAttempted} ({(game.fieldGoalPct * 100).toFixed(1)}%)</span>
                                </div>
                                <div className="text-muted-foreground">
                                  3P: <span className="text-foreground">{game.threePointersMade}-{game.threePointersAttempted} ({(game.threePointPct * 100).toFixed(1)}%)</span>
                                </div>
                                <div className="text-muted-foreground">
                                  FT: <span className="text-foreground">{game.freeThrowsMade}-{game.freeThrowsAttempted} ({(game.freeThrowPct * 100).toFixed(1)}%)</span>
                                </div>
                              </div>
                              <div className="space-y-1 text-xs">
                                <div className="font-semibold text-foreground">Playmaking & Defense</div>
                                <div className="text-muted-foreground">
                                  STL / BLK / TO: <span className="text-foreground">{game.steals} / {game.blocks} / {game.turnovers}</span>
                                </div>
                                <div className="text-muted-foreground">
                                  OREB / DREB: <span className="text-foreground">{game.offensiveRebounds} / {game.defensiveRebounds}</span>
                                </div>
                                <div className="text-muted-foreground">
                                  Fouls / Start: <span className="text-foreground">{game.personalFouls} / {game.startPosition || "—"}</span>
                                </div>
                              </div>
                              <div className="space-y-1 text-xs">
                                <div className="font-semibold text-foreground">Advanced</div>
                                <div className="text-muted-foreground">
                                  TS / eFG / USG: <span className="text-foreground">
                                    {game.tsPct != null ? `${(game.tsPct * 100).toFixed(1)}%` : "—"} / {game.efgPct != null ? `${(game.efgPct * 100).toFixed(1)}%` : "—"} / {game.usgPct != null ? `${(game.usgPct * 100).toFixed(1)}%` : "—"}
                                  </span>
                                </div>
                                <div className="text-muted-foreground">
                                  AST% / REB% / PIE: <span className="text-foreground">
                                    {game.astPct != null ? `${(game.astPct * 100).toFixed(1)}%` : "—"} / {game.rebPct != null ? `${(game.rebPct * 100).toFixed(1)}%` : "—"} / {game.pie != null ? `${(game.pie * 100).toFixed(1)}%` : "—"}
                                  </span>
                                </div>
                                <div className="text-muted-foreground">
                                  ORTG / DRTG / NET: <span className="text-foreground">
                                    {game.offRating != null ? game.offRating.toFixed(1) : "—"} / {game.defRating != null ? game.defRating.toFixed(1) : "—"} / {game.netRating != null ? game.netRating.toFixed(1) : "—"}
                                  </span>
                                </div>
                              </div>
                            </div>
                          </TableCell>
                        </TableRow>
                      )}
                    </>
                  )
                })
              )}
            </TableBody>
          </Table>
        </ScrollArea>
      </CardContent>
    </Card>
  )
}
