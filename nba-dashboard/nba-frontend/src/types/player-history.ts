export type PlayerHistoryGranularity = "game" | "season"

export type PlayerHistoryMetricKey =
  | "points"
  | "rebounds"
  | "assists"
  | "minutes"
  | "fieldGoalPct"
  | "threePointPct"
  | "freeThrowPct"
  | "tsPct"
  | "efgPct"
  | "netRating"
  | "usgPct"
  | "astPct"
  | "rebPct"
  | "pie"

export interface PlayerHistorySearchResult {
  playerId: number
  playerName: string
  position: string
  jerseyNumber: string
  currentTeamId: number | null
  currentTeamName: string
  currentTeamAbbreviation: string
  isActive: boolean
  firstSeasonYear: number
  lastSeasonYear: number
  gamesPlayed: number
}

export interface PlayerHistoryPlayer {
  playerId: number
  playerName: string
  position: string
  jerseyNumber: string
  isActive: boolean
  currentTeamId: number | null
  currentTeamName: string
  currentTeamAbbreviation: string
  firstSeasonYear: number
  lastSeasonYear: number
}

export interface PlayerHistoryMetrics {
  totalGames: number
  seasonsCovered: number
  avgMinutes: number
  avgPoints: number
  avgRebounds: number
  avgAssists: number
}

export interface PlayerHistoryGame {
  gameId: string
  date: string
  seasonYear: number
  teamId: number
  opponentTeamId: number
  isHome: boolean
  won: boolean
  homeTeamId: number
  awayTeamId: number
  homeScore: number
  awayScore: number
  startPosition: string
  minutes: number
  points: number
  rebounds: number
  assists: number
  steals: number
  blocks: number
  turnovers: number
  personalFouls: number
  plusMinus: number
  fieldGoalsMade: number
  fieldGoalsAttempted: number
  fieldGoalPct: number
  threePointersMade: number
  threePointersAttempted: number
  threePointPct: number
  freeThrowsMade: number
  freeThrowsAttempted: number
  freeThrowPct: number
  offensiveRebounds: number
  defensiveRebounds: number
  offRating: number | null
  defRating: number | null
  netRating: number | null
  astPct: number | null
  orebPct: number | null
  drebPct: number | null
  rebPct: number | null
  efgPct: number | null
  tsPct: number | null
  usgPct: number | null
  pace: number | null
  pie: number | null
}

export interface PlayerHistorySeasonDatum {
  seasonYear: number
  seasonLabel: string
  gamesPlayed: number
  minutes: number
  points: number
  rebounds: number
  assists: number
  steals: number
  blocks: number
  turnovers: number
  plusMinus: number
  fieldGoalPct: number
  threePointPct: number
  freeThrowPct: number
  efgPct: number | null
  tsPct: number | null
  netRating: number | null
  usgPct: number | null
  astPct: number | null
  rebPct: number | null
  pie: number | null
}

export interface PlayerHistorySplit {
  key: string
  label: string
  gamesPlayed: number
  minutes: number
  points: number
  rebounds: number
  assists: number
  steals: number
  blocks: number
  turnovers: number
  plusMinus: number
  fieldGoalPct: number
  threePointPct: number
  freeThrowPct: number
  efgPct: number | null
  tsPct: number | null
  netRating: number | null
  usgPct: number | null
  astPct: number | null
  rebPct: number | null
  pie: number | null
}

export interface PlayerHistoryHighlightGame {
  gameId: string
  date: string
  seasonYear: number
  teamId: number
  opponentTeamId: number
  isHome: boolean
  won: boolean
  homeTeamId: number
  awayTeamId: number
  homeScore: number
  awayScore: number
  minutes: number
  points: number
  rebounds: number
  assists: number
  plusMinus: number
  fieldGoalsMade: number
  fieldGoalsAttempted: number
  fieldGoalPct: number
  tsPct: number | null
}

export interface PlayerHistoryHighlights {
  highestPoints: PlayerHistoryHighlightGame | null
  highestRebounds: PlayerHistoryHighlightGame | null
  highestAssists: PlayerHistoryHighlightGame | null
  bestEfficiency: PlayerHistoryHighlightGame | null
  worstShooting: PlayerHistoryHighlightGame | null
  bestPlusMinus: PlayerHistoryHighlightGame | null
  worstPlusMinus: PlayerHistoryHighlightGame | null
}

export interface PlayerHistoryResponse {
  player: PlayerHistoryPlayer
  availableSeasonYears: number[]
  metrics: PlayerHistoryMetrics
  gameLog: PlayerHistoryGame[]
  seasonStats: PlayerHistorySeasonDatum[]
  homeAwaySplits: PlayerHistorySplit[]
  winLossSplits: PlayerHistorySplit[]
  highlights: PlayerHistoryHighlights
}

export interface PlayerHistoryFilter {
  playerId: number | null
  fromSeason: number
  toSeason: number
  granularity: PlayerHistoryGranularity
  metric: PlayerHistoryMetricKey
}
