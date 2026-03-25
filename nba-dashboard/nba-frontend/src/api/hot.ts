import { apiClient } from "./client"
import type { HotPlayersResponse, HotTeamsResponse } from "@/types"

export async function getHotPlayers(window: string): Promise<HotPlayersResponse> {
  const { data } = await apiClient.get<HotPlayersResponse>("/api/hot/players", {
    params: { window },
  })
  return data
}

export async function getHotTeams(window: string): Promise<HotTeamsResponse> {
  const { data } = await apiClient.get<HotTeamsResponse>("/api/hot/teams", {
    params: { window },
  })
  return data
}
