import { useState, useEffect, useMemo } from "react"
import Header from "@/components/layout/Header"
import PageContainer from "@/components/layout/PageContainer"
import FilterBar from "@/components/history/FilterBar"
import SummaryCards from "@/components/history/SummaryCards"
import SeasonBarChart from "@/components/history/SeasonBarChart"
import GameLogPanel from "@/components/history/GameLogPanel"
import MonthlyHeatmap from "@/components/history/MonthlyHeatmap"
import TeamTrajectoryChart from "@/components/history/TeamTrajectoryChart"
import HomeAwayWinRateSplit from "@/components/history/HomeAwayWinRateSplit"
import SeasonIdentityTags from "@/components/history/SeasonIdentityTags"
import BestWorstGamesPanel from "@/components/history/BestWorstGamesPanel"
import ScoringEnvironmentTrend from "@/components/history/ScoringEnvironmentTrend"
import HomeAwayWinRateTrend from "@/components/history/HomeAwayWinRateTrend"
import GameTypeDistribution from "@/components/history/GameTypeDistribution"
import { Skeleton } from "@/components/ui/skeleton"
import { HistoryFilter } from "@/types/history"
import { getHistory, type HistoryResponse } from "@/api/history"
import { getTeamColors } from "@/data/teams"

export const MIN_SEASON = 1996
export const MAX_SEASON = 2025

const DEFAULT_FILTER: HistoryFilter = {
  teamId: null,
  fromSeason: MIN_SEASON,
  toSeason: MAX_SEASON,
}

export default function HistoryPage() {
  const [filter, setFilter] = useState<HistoryFilter>(DEFAULT_FILTER)
  const [data, setData] = useState<HistoryResponse | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false
    setLoading(true)

    getHistory(filter.teamId, filter.fromSeason, filter.toSeason)
      .then((res) => {
        if (!cancelled) setData(res)
      })
      .catch((err) => {
        console.error("Failed to load history", err)
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })

    return () => { cancelled = true }
  }, [filter.teamId, filter.fromSeason, filter.toSeason])

  const heatmapSeasons = useMemo(
    () => Array.from(new Set((data?.heatmapData ?? []).map((c) => c.seasonYear))).sort(),
    [data?.heatmapData]
  )

  const isTeamSelected = filter.teamId !== null
  const isFullRange = filter.fromSeason === MIN_SEASON && filter.toSeason === MAX_SEASON
  const teamColor = isTeamSelected
    ? getTeamColors(filter.teamId!).primary
    : "hsl(var(--primary))"

  return (
    <div>
      <Header />
      <PageContainer>
        <div className="mb-6">
          <h2 className="text-2xl font-semibold tracking-tight">Game History</h2>
          <p className="text-sm text-muted-foreground mt-1">
            Browse and filter historical game records.
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

        {loading || !data ? (
          <div className="space-y-6">
            <div className="grid grid-cols-2 lg:grid-cols-3 gap-4">
              {Array.from({ length: 3 }).map((_, i) => (
                <Skeleton key={i} className="h-[106px] rounded-lg" />
              ))}
            </div>
            <Skeleton className="h-[300px] rounded-lg" />
            <Skeleton className="h-[300px] rounded-lg" />
            <div className="grid grid-cols-1 xl:grid-cols-[55fr_45fr] gap-6">
              <Skeleton className="h-[440px] rounded-lg" />
              <Skeleton className="h-[440px] rounded-lg" />
            </div>
          </div>
        ) : (
          <div className="space-y-6">
            {/* Summary cards — always shown */}
            <SummaryCards metrics={data.metrics} />

            {/* ALL TEAMS mode */}
            {!isTeamSelected && (
              <>
                {isFullRange && <SeasonBarChart data={data.seasonBarData} />}
                <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
                  <ScoringEnvironmentTrend data={data.seasonStats} />
                  <HomeAwayWinRateTrend data={data.seasonStats} />
                </div>
                <GameTypeDistribution data={data.seasonStats} />
              </>
            )}

            {/* TEAM mode */}
            {isTeamSelected && (
              <>
                <TeamTrajectoryChart data={data.seasonStats} teamColor={teamColor} />
                <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
                  <HomeAwayWinRateSplit data={data.seasonStats} teamColor={teamColor} />
                  {data.bestWorstGames && (
                    <BestWorstGamesPanel data={data.bestWorstGames} teamColor={teamColor} />
                  )}
                </div>
                <SeasonIdentityTags data={data.seasonStats} teamColor={teamColor} />
              </>
            )}

            {/* Game log + heatmap — always shown; heatmap only in team mode */}
            <div className={`grid gap-6 ${isTeamSelected ? "grid-cols-1 xl:grid-cols-[55fr_45fr]" : "grid-cols-1"}`}>
              <GameLogPanel
                closestGames={data.closestGames}
                blowoutGames={data.blowoutGames}
              />
              {isTeamSelected && (
                <MonthlyHeatmap data={data.heatmapData} seasonYears={heatmapSeasons} />
              )}
            </div>
          </div>
        )}
      </PageContainer>
    </div>
  )
}
