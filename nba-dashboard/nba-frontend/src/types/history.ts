export interface HistoryGame {
  id: string
  date: string         // "YYYY-MM-DD"
  seasonYear: number   // season start year; 2023 → "2023-24"
  month: number        // calendar month 1–12
  homeTeamId: number
  awayTeamId: number
  homeScore: number
  awayScore: number
}

export interface SeasonBarDatum {
  seasonYear: number
  seasonLabel: string  // "2023-24"
  gameCount: number
  annotation?: string  // "lockout" | "bubble"
}

export interface HeatmapCell {
  seasonYear: number
  month: number
  wins: number
  losses: number
  winPct: number | null // 0–1; null if no games
}

export interface HistoryMetrics {
  totalGames: number
  seasonsCovered: number
  avgMarginOfVictory: number
}

export interface SeasonStatDatum {
  seasonYear: number
  seasonLabel: string
  gameCount: number
  // Team mode (null when all teams)
  winPct: number | null
  homeWinPct: number | null
  awayWinPct: number | null
  // All-teams mode (null when team selected)
  avgTotalPoints: number | null
  leagueHomeWinPct: number | null
  // Both modes
  closeGames: number
  moderateGames: number
  blowoutGames: number
}

export interface BestWorstGames {
  largestWin: HistoryGame | null
  largestLoss: HistoryGame | null
  highestScoringGame: HistoryGame | null
  lowestScoringGame: HistoryGame | null
}

export interface HistoryFilter {
  teamId: number | null
  fromSeason: number
  toSeason: number
}

export type SortField = "date" | "margin"
export type SortDirection = "asc" | "desc"
