export interface Team {
  id: number
  name: string
  fullName: string
  abbreviation: string
  city: string
  conference: string
  division: string
}

export interface Game {
  id: string
  date: string
  status: "Scheduled" | "In Progress" | "Final"
  homeTeamId: number
  visitorTeamId: number
  homeScore: number
  visitorScore: number
  arena: string
}

export interface UpcomingGame {
  game: Game
  homeTeam: Team
  visitorTeam: Team
}

export interface PlayerGameStats {
  playerId: number
  playerName: string
  position: string
  jerseyNumber: string
  minutes: number
  points: number
  rebounds: number
  assists: number
  steals: number
  blocks: number
  turnovers: number
  fieldGoalPct: number
  threePointPct: number
  freeThrowPct: number
  plusMinus: number
}

export interface PlayerSeasonAvg {
  playerId: number
  playerName: string
  position: string
  jerseyNumber: string
  gamesPlayed: number
  ptsAvg: number
  rebAvg: number
  astAvg: number
  stlAvg: number
  blkAvg: number
  fgPct: number
  fg3Pct: number
  tsPct: number
}

export interface MatchupGame {
  game: Game
  homeTeam: Team
  visitorTeam: Team
  homePlayerStats: PlayerGameStats[]
  visitorPlayerStats: PlayerGameStats[]
}

export interface MatchupHistory {
  team: Team
  opponent: Team
  games: MatchupGame[]
  teamWins: number
  opponentWins: number
}

export interface StandingsEntry {
  team: Team
  conference: string
  wins: number
  losses: number
  winPct: number
  confRank: number
  divRank: number
  homeRecord: string
  awayRecord: string
  last10: string
  streak: string
  offRating: number
  defRating: number
  netRating: number
  pace: number
}

export interface OutperformingPlayer {
  playerId: number
  playerName: string
  position: string
  jerseyNumber: string
  teamId: number
  vsOpponent: {
    ptsAvg: number
    rebAvg: number
    astAvg: number
    fgPct: number
    gamesPlayed: number
  }
  seasonAvg: {
    ptsAvg: number
    rebAvg: number
    astAvg: number
    fgPct: number
    gamesPlayed: number
  }
  delta: {
    pts: number
    reb: number
    ast: number
    fgPct: number
  }
}
