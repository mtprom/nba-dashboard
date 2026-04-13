import { apiClient } from "./client"
import type {
  PlayerHistoryResponse,
  PlayerHistorySearchResult,
} from "@/types/player-history"

export async function searchPlayers(query: string): Promise<PlayerHistorySearchResult[]> {
  const { data } = await apiClient.get<PlayerHistorySearchResult[]>("/api/players/search", {
    params: { query },
  })
  return data
}

export async function getPlayerHistory(
  playerId: number,
  fromSeason: number,
  toSeason: number,
): Promise<PlayerHistoryResponse> {
  const { data } = await apiClient.get<PlayerHistoryResponse>("/api/players/history", {
    params: { playerId, fromSeason, toSeason },
  })
  return data
}
