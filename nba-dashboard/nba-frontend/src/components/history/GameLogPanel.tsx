import { useState } from "react"
import { ChevronUp, ChevronDown, ChevronsUpDown } from "lucide-react"
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card"
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/tabs"
import { ScrollArea } from "@/components/ui/scroll-area"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { HistoryGame, SortField, SortDirection } from "@/types/history"
import { getMargin } from "@/data/mock-history"
import { TEAM_INFO, getTeamColorsDark } from "@/data/teams"

interface GameLogPanelProps {
  closestGames: HistoryGame[]
  blowoutGames: HistoryGame[]
}

type Tab = "closest" | "blowouts"

function TeamCell({ teamId }: { teamId: number }) {
  const { primary } = getTeamColorsDark(teamId)
  const abbr = TEAM_INFO[teamId]?.abbreviation ?? "???"
  return (
    <div className="flex items-center gap-1.5">
      <span
        className="inline-block h-2.5 w-2.5 rounded-full flex-shrink-0"
        style={{ backgroundColor: primary }}
      />
      <span className="text-xs font-medium">{abbr}</span>
    </div>
  )
}

export function formatDate(iso: string): string {
  return new Date(iso + "T12:00:00").toLocaleDateString("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric",
  })
}

function SortIcon({ field, activeField, direction }: { field: SortField; activeField: SortField; direction: SortDirection }) {
  if (field !== activeField) return <ChevronsUpDown className="inline h-3 w-3 ml-0.5 opacity-40" />
  return direction === "asc"
    ? <ChevronUp className="inline h-3 w-3 ml-0.5" />
    : <ChevronDown className="inline h-3 w-3 ml-0.5" />
}

function GameTable({ games }: { games: HistoryGame[] }) {
  const [sortField, setSortField] = useState<SortField>("date")
  const [sortDir, setSortDir] = useState<SortDirection>("desc")

  function handleSort(field: SortField) {
    if (sortField === field) {
      setSortDir((d) => (d === "asc" ? "desc" : "asc"))
    } else {
      setSortField(field)
      setSortDir("desc")
    }
  }

  const sorted = [...games].sort((a, b) => {
    let cmp = 0
    if (sortField === "date") cmp = a.date.localeCompare(b.date)
    else cmp = getMargin(a) - getMargin(b)
    return sortDir === "asc" ? cmp : -cmp
  })

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead
            className="cursor-pointer select-none whitespace-nowrap w-[110px]"
            onClick={() => handleSort("date")}
          >
            Date <SortIcon field="date" activeField={sortField} direction={sortDir} />
          </TableHead>
          <TableHead className="w-[52px]">Home</TableHead>
          <TableHead className="text-center w-[80px]">Score</TableHead>
          <TableHead className="w-[52px]">Away</TableHead>
          <TableHead
            className="cursor-pointer select-none text-right w-[72px]"
            onClick={() => handleSort("margin")}
          >
            Margin <SortIcon field="margin" activeField={sortField} direction={sortDir} />
          </TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {sorted.length === 0 ? (
          <TableRow>
            <TableCell colSpan={5} className="text-center text-muted-foreground text-sm py-8">
              No games match current filters.
            </TableCell>
          </TableRow>
        ) : (
          sorted.map((g) => {
            const homeWon = g.homeScore > g.awayScore
            return (
              <TableRow key={g.id}>
                <TableCell className="text-xs text-muted-foreground whitespace-nowrap">
                  {formatDate(g.date)}
                </TableCell>
                <TableCell>
                  <TeamCell teamId={g.homeTeamId} />
                </TableCell>
                <TableCell className="text-center text-xs font-mono font-medium">
                  <span className={homeWon ? "text-foreground" : "text-muted-foreground"}>
                    {g.homeScore}
                  </span>
                  <span className="text-muted-foreground mx-0.5">–</span>
                  <span className={!homeWon ? "text-foreground" : "text-muted-foreground"}>
                    {g.awayScore}
                  </span>
                </TableCell>
                <TableCell>
                  <TeamCell teamId={g.awayTeamId} />
                </TableCell>
                <TableCell className="text-right text-xs text-muted-foreground">
                  {getMargin(g)} pts
                </TableCell>
              </TableRow>
            )
          })
        )}
      </TableBody>
    </Table>
  )
}

export default function GameLogPanel({ closestGames, blowoutGames }: GameLogPanelProps) {
  const [activeTab, setActiveTab] = useState<Tab>("closest")

  const tabGames: Record<Tab, HistoryGame[]> = {
    closest: closestGames,
    blowouts: blowoutGames,
  }

  return (
    <Card className="flex flex-col">
      <CardHeader className="pb-2">
        <CardTitle className="text-base font-medium">Game Log</CardTitle>
      </CardHeader>
      <CardContent className="flex-1 p-0 px-6 pb-6">
        <Tabs value={activeTab} onValueChange={(v) => setActiveTab(v as Tab)}>
          <TabsList className="mb-3">
            <TabsTrigger value="closest">Closest</TabsTrigger>
            <TabsTrigger value="blowouts">Blowouts</TabsTrigger>
          </TabsList>
          {(["closest", "blowouts"] as Tab[]).map((tab) => (
            <TabsContent key={tab} value={tab} className="mt-0">
              <ScrollArea className="h-[360px]">
                <GameTable games={tabGames[tab]} />
              </ScrollArea>
            </TabsContent>
          ))}
        </Tabs>
      </CardContent>
    </Card>
  )
}
