import { apiClient } from "./client"
import type { StandingsEntry } from "@/types"

export async function getStandings(): Promise<StandingsEntry[]> {
  const { data } = await apiClient.get<StandingsEntry[]>("/api/standings")
  return data
}
