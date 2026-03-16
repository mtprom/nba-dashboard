import type { UpcomingGame, MatchupHistory, PlayerSeasonAvg } from "@/types"
import { MOCK_UPCOMING_GAMES } from "@/data/mock-upcoming-games"
import { MOCK_MATCHUP_HISTORY, MOCK_SEASON_AVERAGES } from "@/data/mock-matchup-history"

export async function getUpcomingGames(): Promise<UpcomingGame[]> {
  return MOCK_UPCOMING_GAMES
}

export async function getMatchupHistory(
  teamId: number,
  opponentId: number
): Promise<MatchupHistory | null> {
  const k = [teamId, opponentId].sort((a, b) => a - b).join("-")
  return MOCK_MATCHUP_HISTORY[k] ?? null
}

export async function getSeasonAverages(): Promise<Record<number, PlayerSeasonAvg>> {
  return MOCK_SEASON_AVERAGES
}
