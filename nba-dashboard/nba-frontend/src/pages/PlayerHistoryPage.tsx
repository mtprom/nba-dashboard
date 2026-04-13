import { useDeferredValue, useEffect, useMemo, useState } from "react"
import Header from "@/components/layout/Header"
import PageContainer from "@/components/layout/PageContainer"
import PlayerBestWorstPanel from "@/components/player-history/PlayerBestWorstPanel"
import PlayerGameLogPanel from "@/components/player-history/PlayerGameLogPanel"
import PlayerHeaderCard from "@/components/player-history/PlayerHeaderCard"
import PlayerHistoryFilterBar from "@/components/player-history/PlayerHistoryFilterBar"
import PlayerRoleUsageChart from "@/components/player-history/PlayerRoleUsageChart"
import PlayerSeasonBarChart from "@/components/player-history/PlayerSeasonBarChart"
import PlayerSplitChart from "@/components/player-history/PlayerSplitChart"
import PlayerSummaryCards from "@/components/player-history/PlayerSummaryCards"
import PlayerTrendChart from "@/components/player-history/PlayerTrendChart"
import { Skeleton } from "@/components/ui/skeleton"
import { getPlayerHistory, searchPlayers } from "@/api/player-history"
import { seasonRange } from "@/data/player-history"
import { getTeamColorsDark } from "@/data/teams"
import type {
  PlayerHistoryFilter,
  PlayerHistoryResponse,
  PlayerHistorySearchResult,
} from "@/types/player-history"

const DEFAULT_FILTER: PlayerHistoryFilter = {
  playerId: null,
  fromSeason: 1996,
  toSeason: 2025,
  granularity: "season",
  metric: "points",
}

export default function PlayerHistoryPage() {
  const [filter, setFilter] = useState<PlayerHistoryFilter>(DEFAULT_FILTER)
  const [searchQuery, setSearchQuery] = useState("")
  const deferredQuery = useDeferredValue(searchQuery)
  const [searchResults, setSearchResults] = useState<PlayerHistorySearchResult[]>([])
  const [selectedPlayer, setSelectedPlayer] = useState<PlayerHistorySearchResult | null>(null)
  const [searching, setSearching] = useState(false)
  const [loading, setLoading] = useState(false)
  const [data, setData] = useState<PlayerHistoryResponse | null>(null)

  useEffect(() => {
    const query = deferredQuery.trim()
    const selectedName = selectedPlayer?.playerName ?? ""

    if (query.length < 2 || query === selectedName) {
      setSearchResults([])
      setSearching(false)
      return
    }

    let cancelled = false
    setSearching(true)

    searchPlayers(query)
      .then((results) => {
        if (!cancelled) setSearchResults(results)
      })
      .catch((error) => {
        console.error("Failed to search players", error)
        if (!cancelled) setSearchResults([])
      })
      .finally(() => {
        if (!cancelled) setSearching(false)
      })

    return () => {
      cancelled = true
    }
  }, [deferredQuery, selectedPlayer?.playerName])

  useEffect(() => {
    if (filter.playerId === null) {
      setData(null)
      setLoading(false)
      return
    }

    let cancelled = false
    setLoading(true)

    getPlayerHistory(filter.playerId, filter.fromSeason, filter.toSeason)
      .then((response) => {
        if (!cancelled) setData(response)
      })
      .catch((error) => {
        console.error("Failed to load player history", error)
        if (!cancelled) setData(null)
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [filter.playerId, filter.fromSeason, filter.toSeason])

  const availableSeasonYears = useMemo(() => {
    if (data?.availableSeasonYears?.length) return data.availableSeasonYears
    if (selectedPlayer) return seasonRange(selectedPlayer.firstSeasonYear, selectedPlayer.lastSeasonYear)
    return []
  }, [data?.availableSeasonYears, selectedPlayer])

  const accentColor = data?.player.currentTeamId != null
    ? getTeamColorsDark(data.player.currentTeamId).primary
    : selectedPlayer?.currentTeamId != null
      ? getTeamColorsDark(selectedPlayer.currentTeamId).primary
      : "hsl(var(--primary))"

  function handleSelectPlayer(player: PlayerHistorySearchResult) {
    const granularity = player.firstSeasonYear === player.lastSeasonYear ? "game" : "season"
    setSelectedPlayer(player)
    setSearchQuery(player.playerName)
    setSearchResults([])
    setFilter((current) => ({
      ...current,
      playerId: player.playerId,
      fromSeason: player.firstSeasonYear,
      toSeason: player.lastSeasonYear,
      granularity,
    }))
  }

  function handleReset() {
    setSelectedPlayer(null)
    setSearchQuery("")
    setSearchResults([])
    setData(null)
    setFilter(DEFAULT_FILTER)
  }

  return (
    <div>
      <Header />
      <PageContainer>
        <div className="mb-6">
          <h2 className="text-2xl font-semibold tracking-tight">Player History</h2>
          <p className="mt-1 text-sm text-muted-foreground">
            Browse player box scores and season-level trends across any available range.
          </p>
        </div>

        <div className="mb-6">
          <PlayerHistoryFilterBar
            filter={filter}
            searchQuery={searchQuery}
            onSearchQueryChange={setSearchQuery}
            onSelectPlayer={handleSelectPlayer}
            searchResults={searchResults}
            availableSeasonYears={availableSeasonYears}
            onFilterChange={setFilter}
            onReset={handleReset}
            isSearching={searching}
          />
        </div>

        {filter.playerId === null ? (
          <div className="rounded-lg border border-dashed border-border bg-card p-8 text-center">
            <h3 className="text-lg font-medium">Select a player to begin</h3>
            <p className="mt-2 text-sm text-muted-foreground">
              Search for any player already ingested into the database, then adjust the season range or chart view.
            </p>
          </div>
        ) : loading || !data ? (
          <div className="space-y-6">
            <Skeleton className="h-[120px] rounded-lg" />
            <div className="grid grid-cols-2 gap-4 xl:grid-cols-6">
              {Array.from({ length: 6 }).map((_, index) => (
                <Skeleton key={index} className="h-[112px] rounded-lg" />
              ))}
            </div>
            <Skeleton className="h-[340px] rounded-lg" />
            <div className="grid grid-cols-1 gap-6 xl:grid-cols-2">
              <Skeleton className="h-[320px] rounded-lg" />
              <Skeleton className="h-[320px] rounded-lg" />
            </div>
            <Skeleton className="h-[540px] rounded-lg" />
          </div>
        ) : data.gameLog.length === 0 ? (
          <div className="rounded-lg border border-border bg-card p-8 text-center">
            <h3 className="text-lg font-medium">No games in this range</h3>
            <p className="mt-2 text-sm text-muted-foreground">
              The selected player has no qualifying regular-season box scores for these seasons.
            </p>
          </div>
        ) : (
          <div className="space-y-6">
            <PlayerHeaderCard player={data.player} accentColor={accentColor} />
            <PlayerSummaryCards metrics={data.metrics} />
            <PlayerTrendChart
              games={data.gameLog}
              seasonStats={data.seasonStats}
              metric={filter.metric}
              granularity={filter.granularity}
              onMetricChange={(metric) => setFilter((current) => ({ ...current, metric }))}
              accentColor={accentColor}
            />
            <div className="grid grid-cols-1 gap-6 xl:grid-cols-2">
              <PlayerSeasonBarChart
                seasonStats={data.seasonStats}
                metric={filter.metric}
                accentColor={accentColor}
              />
              <PlayerRoleUsageChart
                seasonStats={data.seasonStats}
                accentColor={accentColor}
              />
            </div>
            <div className="grid grid-cols-1 gap-6 xl:grid-cols-2">
              <PlayerSplitChart
                title="Home vs Away"
                data={data.homeAwaySplits}
                metric={filter.metric}
                accentColor={accentColor}
              />
              <PlayerSplitChart
                title="Wins vs Losses"
                data={data.winLossSplits}
                metric={filter.metric}
                accentColor={accentColor}
              />
            </div>
            <PlayerBestWorstPanel highlights={data.highlights} accentColor={accentColor} />
            <PlayerGameLogPanel games={data.gameLog} />
          </div>
        )}
      </PageContainer>
    </div>
  )
}
