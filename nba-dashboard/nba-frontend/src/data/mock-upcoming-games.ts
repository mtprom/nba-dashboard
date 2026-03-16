import type { UpcomingGame } from "@/types"
import { TEAM_INFO } from "./teams"

function team(id: number) {
  const t = TEAM_INFO[id]
  return { id, ...t }
}

export const MOCK_UPCOMING_GAMES: UpcomingGame[] = [
  {
    game: {
      id: "0022500892",
      date: "2026-03-16T19:30:00Z",
      status: "Scheduled",
      homeTeamId: 1610612738,
      visitorTeamId: 1610612747,
      homeScore: 0,
      visitorScore: 0,
      arena: "TD Garden",
    },
    homeTeam: team(1610612738),   // Celtics
    visitorTeam: team(1610612747), // Lakers
  },
  {
    game: {
      id: "0022500893",
      date: "2026-03-16T20:00:00Z",
      status: "Scheduled",
      homeTeamId: 1610612752,
      visitorTeamId: 1610612744,
      homeScore: 0,
      visitorScore: 0,
      arena: "Madison Square Garden",
    },
    homeTeam: team(1610612752),   // Knicks
    visitorTeam: team(1610612744), // Warriors
  },
  {
    game: {
      id: "0022500894",
      date: "2026-03-16T20:30:00Z",
      status: "Scheduled",
      homeTeamId: 1610612748,
      visitorTeamId: 1610612749,
      homeScore: 0,
      visitorScore: 0,
      arena: "Kaseya Center",
    },
    homeTeam: team(1610612748),   // Heat
    visitorTeam: team(1610612749), // Bucks
  },
  {
    game: {
      id: "0022500895",
      date: "2026-03-16T21:00:00Z",
      status: "Scheduled",
      homeTeamId: 1610612742,
      visitorTeamId: 1610612743,
      homeScore: 0,
      visitorScore: 0,
      arena: "American Airlines Center",
    },
    homeTeam: team(1610612742),   // Mavericks
    visitorTeam: team(1610612743), // Nuggets
  },
  {
    game: {
      id: "0022500896",
      date: "2026-03-16T22:00:00Z",
      status: "Scheduled",
      homeTeamId: 1610612756,
      visitorTeamId: 1610612760,
      homeScore: 0,
      visitorScore: 0,
      arena: "Footprint Center",
    },
    homeTeam: team(1610612756),   // Suns
    visitorTeam: team(1610612760), // Thunder
  },
  {
    game: {
      id: "0022500897",
      date: "2026-03-16T22:30:00Z",
      status: "Scheduled",
      homeTeamId: 1610612746,
      visitorTeamId: 1610612739,
      homeScore: 0,
      visitorScore: 0,
      arena: "Intuit Dome",
    },
    homeTeam: team(1610612746),   // Clippers
    visitorTeam: team(1610612739), // Cavaliers
  },
]
