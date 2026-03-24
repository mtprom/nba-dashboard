import { apiClient } from "./client"
import type { HotPlayer, HotTeam } from "@/types"

export async function getHotPlayers(window: string): Promise<HotPlayer[]> {
  const { data } = await apiClient.get<HotPlayer[]>("/api/hot/players", {
    params: { window },
  })
  return data
}

export async function getHotTeams(window: string): Promise<HotTeam[]> {
  const { data } = await apiClient.get<HotTeam[]>("/api/hot/teams", {
    params: { window },
  })
  return data
}
