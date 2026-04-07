export interface HistoryGame {
  id: string
  date: string         // "YYYY-MM-DD"
  seasonYear: number   // season start year; 2023 → "2023-24"
  month: number        // calendar month 1–12
  homeTeamId: number
  awayTeamId: number
  homeScore: number
  awayScore: number
  isOT: boolean
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
  winPct: number       // 0–1; NaN if no games
}

export interface HistoryMetrics {
  totalGames: number
  seasonsCovered: number
  avgMarginOfVictory: number
  overtimeRate: number  // percentage e.g. 10.6
}

export interface HistoryFilter {
  teamId: number | null
  fromSeason: number
  toSeason: number
}

export type SortField = "date" | "margin"
export type SortDirection = "asc" | "desc"
