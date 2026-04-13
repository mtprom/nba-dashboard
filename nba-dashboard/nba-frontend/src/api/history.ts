import { apiClient } from "./client"
import type {
  HistoryMetrics,
  SeasonBarDatum,
  HeatmapCell,
  HistoryGame,
  SeasonStatDatum,
  BestWorstGames,
  LeaguePlacement,
} from "@/types/history"

export interface HistoryResponse {
  metrics: HistoryMetrics
  seasonBarData: SeasonBarDatum[]
  heatmapData: HeatmapCell[]
  closestGames: HistoryGame[]
  blowoutGames: HistoryGame[]
  seasonStats: SeasonStatDatum[]
  bestWorstGames: BestWorstGames | null
  leaguePlacement: LeaguePlacement | null
}

export async function getHistory(
  teamId: number | null,
  fromSeason: number,
  toSeason: number
): Promise<HistoryResponse> {
  const params: Record<string, number> = { fromSeason, toSeason }
  if (teamId !== null) params.teamId = teamId
  const { data } = await apiClient.get<HistoryResponse>("/api/history", { params })
  return data
}
