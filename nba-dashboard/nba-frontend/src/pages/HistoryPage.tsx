import { useState, useMemo } from "react"
import Header from "@/components/layout/Header"
import PageContainer from "@/components/layout/PageContainer"
import FilterBar from "@/components/history/FilterBar"
import SummaryCards from "@/components/history/SummaryCards"
import SeasonBarChart from "@/components/history/SeasonBarChart"
import GameLogPanel from "@/components/history/GameLogPanel"
import MonthlyHeatmap from "@/components/history/MonthlyHeatmap"
import { HistoryFilter } from "@/types/history"
import {
  MOCK_GAMES,
  filterGames,
  computeMetrics,
  buildSeasonBarData,
  buildHeatmapData,
  getClosestGames,
  getBlowouts,
  getOTGames,
} from "@/data/mock-history"

const DEFAULT_FILTER: HistoryFilter = {
  teamId: null,
  fromSeason: 1994,
  toSeason: 2024,
}

export default function HistoryPage() {
  const [filter, setFilter] = useState<HistoryFilter>(DEFAULT_FILTER)

  const filteredGames = useMemo(() => filterGames(MOCK_GAMES, filter), [filter])
  const metrics = useMemo(() => computeMetrics(filteredGames), [filteredGames])
  const seasonBarData = useMemo(
    () => buildSeasonBarData(filteredGames, filter.fromSeason, filter.toSeason),
    [filteredGames, filter.fromSeason, filter.toSeason]
  )
  const heatmapData = useMemo(() => buildHeatmapData(filteredGames), [filteredGames])
  const closestGames = useMemo(() => getClosestGames(filteredGames), [filteredGames])
  const blowoutGames = useMemo(() => getBlowouts(filteredGames), [filteredGames])
  const otGames = useMemo(() => getOTGames(filteredGames), [filteredGames])

  // Unique season years present in filtered games, for heatmap row rendering
  const heatmapSeasons = useMemo(
    () => Array.from(new Set(filteredGames.map((g) => g.seasonYear))).sort(),
    [filteredGames]
  )

  return (
    <div>
      <Header />
      <PageContainer>
        <div className="mb-6">
          <h2 className="text-2xl font-semibold tracking-tight">Game History</h2>
          <p className="text-sm text-muted-foreground mt-1">
            Browse and filter game records from 1994 through 2024.
          </p>
        </div>

        {/* Filter bar */}
        <div className="mb-6">
          <FilterBar
            filter={filter}
            onFilterChange={setFilter}
            onReset={() => setFilter(DEFAULT_FILTER)}
          />
        </div>

        {/* Summary cards */}
        <div className="mb-6">
          <SummaryCards metrics={metrics} />
        </div>

        {/* Season bar chart */}
        <div className="mb-6">
          <SeasonBarChart data={seasonBarData} />
        </div>

        {/* Game log + heatmap side by side */}
        <div className="grid grid-cols-1 xl:grid-cols-[55fr_45fr] gap-6">
          <GameLogPanel
            closestGames={closestGames}
            blowoutGames={blowoutGames}
            otGames={otGames}
          />
          <MonthlyHeatmap data={heatmapData} seasonYears={heatmapSeasons} />
        </div>
      </PageContainer>
    </div>
  )
}
