import { apiClient } from "./client"
import type { UpcomingGame, MatchupHistory, PlayerSeasonAvg } from "@/types"

export async function getUpcomingGames(): Promise<UpcomingGame[]> {
  const { data } = await apiClient.get<UpcomingGame[]>("/api/games/upcoming")
  return data
}

export async function getMatchupHistory(
  teamId: number,
  opponentId: number
): Promise<MatchupHistory> {
  const { data } = await apiClient.get<MatchupHistory>(
    `/api/teams/${teamId}/matchup/${opponentId}`
  )
  return data
}

export async function getSeasonAverages(
  teamIds: number[]
): Promise<Record<number, PlayerSeasonAvg>> {
  const { data } = await apiClient.get<Record<number, PlayerSeasonAvg>>(
    "/api/players/season-averages",
    { params: { teamIds: teamIds.join(",") } }
  )
  return data
}
