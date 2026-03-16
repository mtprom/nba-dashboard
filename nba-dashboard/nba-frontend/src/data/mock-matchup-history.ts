import type { MatchupHistory, PlayerSeasonAvg } from "@/types"
import { TEAM_INFO } from "./teams"

function team(id: number) {
  const t = TEAM_INFO[id]
  return { id, ...t }
}

const BOS = 1610612738
const LAL = 1610612747
const NYK = 1610612752
const GSW = 1610612744
const MIA = 1610612748
const MIL = 1610612749
const DAL = 1610612742
const DEN = 1610612743
const PHX = 1610612756
const OKC = 1610612760
const LAC = 1610612746
const CLE = 1610612739

function key(a: number, b: number) {
  return [a, b].sort((x, y) => x - y).join("-")
}

export const MOCK_MATCHUP_HISTORY: Record<string, MatchupHistory> = {
  // Celtics vs Lakers
  [key(BOS, LAL)]: {
    team: team(BOS),
    opponent: team(LAL),
    teamWins: 3,
    opponentWins: 2,
    games: [
      {
        game: { id: "0022500456", date: "2026-02-14", status: "Final", homeTeamId: LAL, visitorTeamId: BOS, homeScore: 108, visitorScore: 117, arena: "Crypto.com Arena" },
        homeTeam: team(LAL),
        visitorTeam: team(BOS),
        homePlayerStats: [
          { playerId: 2544, playerName: "LeBron James", position: "SF", jerseyNumber: "23", minutes: 36, points: 28, rebounds: 8, assists: 9, steals: 1, blocks: 1, turnovers: 4, fieldGoalPct: 0.478, threePointPct: 0.333, freeThrowPct: 0.857, plusMinus: -5 },
          { playerId: 203076, playerName: "Anthony Davis", position: "PF", jerseyNumber: "3", minutes: 38, points: 32, rebounds: 14, assists: 3, steals: 2, blocks: 3, turnovers: 2, fieldGoalPct: 0.538, threePointPct: 0.250, freeThrowPct: 0.800, plusMinus: -3 },
          { playerId: 1629629, playerName: "Austin Reaves", position: "SG", jerseyNumber: "15", minutes: 34, points: 18, rebounds: 4, assists: 6, steals: 1, blocks: 0, turnovers: 2, fieldGoalPct: 0.421, threePointPct: 0.375, freeThrowPct: 0.900, plusMinus: -8 },
        ],
        visitorPlayerStats: [
          { playerId: 1628369, playerName: "Jayson Tatum", position: "SF", jerseyNumber: "0", minutes: 38, points: 38, rebounds: 9, assists: 6, steals: 2, blocks: 0, turnovers: 3, fieldGoalPct: 0.520, threePointPct: 0.444, freeThrowPct: 0.909, plusMinus: 12 },
          { playerId: 1628370, playerName: "Jaylen Brown", position: "SG", jerseyNumber: "7", minutes: 36, points: 26, rebounds: 5, assists: 4, steals: 1, blocks: 1, turnovers: 2, fieldGoalPct: 0.480, threePointPct: 0.400, freeThrowPct: 0.833, plusMinus: 8 },
          { playerId: 203935, playerName: "Jrue Holiday", position: "PG", jerseyNumber: "4", minutes: 34, points: 18, rebounds: 6, assists: 8, steals: 3, blocks: 0, turnovers: 1, fieldGoalPct: 0.450, threePointPct: 0.333, freeThrowPct: 1.000, plusMinus: 10 },
        ],
      },
      {
        game: { id: "0022500321", date: "2026-01-20", status: "Final", homeTeamId: BOS, visitorTeamId: LAL, homeScore: 122, visitorScore: 118, arena: "TD Garden" },
        homeTeam: team(BOS),
        visitorTeam: team(LAL),
        homePlayerStats: [
          { playerId: 1628369, playerName: "Jayson Tatum", position: "SF", jerseyNumber: "0", minutes: 40, points: 42, rebounds: 7, assists: 5, steals: 1, blocks: 1, turnovers: 4, fieldGoalPct: 0.560, threePointPct: 0.500, freeThrowPct: 0.889, plusMinus: 6 },
          { playerId: 1628370, playerName: "Jaylen Brown", position: "SG", jerseyNumber: "7", minutes: 37, points: 24, rebounds: 6, assists: 3, steals: 2, blocks: 0, turnovers: 1, fieldGoalPct: 0.455, threePointPct: 0.364, freeThrowPct: 0.750, plusMinus: 4 },
          { playerId: 203935, playerName: "Jrue Holiday", position: "PG", jerseyNumber: "4", minutes: 35, points: 22, rebounds: 4, assists: 10, steals: 2, blocks: 1, turnovers: 2, fieldGoalPct: 0.500, threePointPct: 0.400, freeThrowPct: 0.857, plusMinus: 5 },
        ],
        visitorPlayerStats: [
          { playerId: 2544, playerName: "LeBron James", position: "SF", jerseyNumber: "23", minutes: 38, points: 34, rebounds: 10, assists: 8, steals: 2, blocks: 0, turnovers: 5, fieldGoalPct: 0.500, threePointPct: 0.286, freeThrowPct: 0.800, plusMinus: -2 },
          { playerId: 203076, playerName: "Anthony Davis", position: "PF", jerseyNumber: "3", minutes: 40, points: 36, rebounds: 12, assists: 2, steals: 1, blocks: 4, turnovers: 3, fieldGoalPct: 0.571, threePointPct: 0.333, freeThrowPct: 0.750, plusMinus: -1 },
          { playerId: 1629629, playerName: "Austin Reaves", position: "SG", jerseyNumber: "15", minutes: 36, points: 22, rebounds: 3, assists: 7, steals: 0, blocks: 0, turnovers: 3, fieldGoalPct: 0.450, threePointPct: 0.400, freeThrowPct: 1.000, plusMinus: -5 },
        ],
      },
      {
        game: { id: "0022400987", date: "2025-12-08", status: "Final", homeTeamId: LAL, visitorTeamId: BOS, homeScore: 115, visitorScore: 110, arena: "Crypto.com Arena" },
        homeTeam: team(LAL),
        visitorTeam: team(BOS),
        homePlayerStats: [
          { playerId: 2544, playerName: "LeBron James", position: "SF", jerseyNumber: "23", minutes: 37, points: 30, rebounds: 7, assists: 11, steals: 1, blocks: 2, turnovers: 3, fieldGoalPct: 0.520, threePointPct: 0.400, freeThrowPct: 0.800, plusMinus: 8 },
          { playerId: 203076, playerName: "Anthony Davis", position: "PF", jerseyNumber: "3", minutes: 39, points: 28, rebounds: 15, assists: 4, steals: 1, blocks: 2, turnovers: 1, fieldGoalPct: 0.500, threePointPct: 0.000, freeThrowPct: 0.857, plusMinus: 7 },
          { playerId: 1629629, playerName: "Austin Reaves", position: "SG", jerseyNumber: "15", minutes: 35, points: 20, rebounds: 5, assists: 5, steals: 2, blocks: 0, turnovers: 1, fieldGoalPct: 0.444, threePointPct: 0.333, freeThrowPct: 0.875, plusMinus: 3 },
        ],
        visitorPlayerStats: [
          { playerId: 1628369, playerName: "Jayson Tatum", position: "SF", jerseyNumber: "0", minutes: 39, points: 30, rebounds: 8, assists: 4, steals: 0, blocks: 1, turnovers: 3, fieldGoalPct: 0.440, threePointPct: 0.375, freeThrowPct: 0.900, plusMinus: -4 },
          { playerId: 1628370, playerName: "Jaylen Brown", position: "SG", jerseyNumber: "7", minutes: 37, points: 28, rebounds: 7, assists: 5, steals: 1, blocks: 0, turnovers: 2, fieldGoalPct: 0.500, threePointPct: 0.429, freeThrowPct: 0.800, plusMinus: -2 },
          { playerId: 203935, playerName: "Jrue Holiday", position: "PG", jerseyNumber: "4", minutes: 33, points: 15, rebounds: 5, assists: 7, steals: 2, blocks: 0, turnovers: 1, fieldGoalPct: 0.375, threePointPct: 0.250, freeThrowPct: 1.000, plusMinus: -6 },
        ],
      },
      {
        game: { id: "0022400765", date: "2025-11-15", status: "Final", homeTeamId: BOS, visitorTeamId: LAL, homeScore: 125, visitorScore: 112, arena: "TD Garden" },
        homeTeam: team(BOS),
        visitorTeam: team(LAL),
        homePlayerStats: [
          { playerId: 1628369, playerName: "Jayson Tatum", position: "SF", jerseyNumber: "0", minutes: 36, points: 35, rebounds: 11, assists: 7, steals: 1, blocks: 0, turnovers: 2, fieldGoalPct: 0.538, threePointPct: 0.455, freeThrowPct: 0.857, plusMinus: 15 },
          { playerId: 1628370, playerName: "Jaylen Brown", position: "SG", jerseyNumber: "7", minutes: 34, points: 22, rebounds: 4, assists: 3, steals: 2, blocks: 1, turnovers: 1, fieldGoalPct: 0.440, threePointPct: 0.333, freeThrowPct: 0.800, plusMinus: 12 },
          { playerId: 203935, playerName: "Jrue Holiday", position: "PG", jerseyNumber: "4", minutes: 32, points: 20, rebounds: 3, assists: 9, steals: 1, blocks: 0, turnovers: 2, fieldGoalPct: 0.471, threePointPct: 0.400, freeThrowPct: 0.833, plusMinus: 14 },
        ],
        visitorPlayerStats: [
          { playerId: 2544, playerName: "LeBron James", position: "SF", jerseyNumber: "23", minutes: 35, points: 26, rebounds: 6, assists: 7, steals: 0, blocks: 1, turnovers: 4, fieldGoalPct: 0.450, threePointPct: 0.250, freeThrowPct: 0.875, plusMinus: -10 },
          { playerId: 203076, playerName: "Anthony Davis", position: "PF", jerseyNumber: "3", minutes: 37, points: 30, rebounds: 10, assists: 2, steals: 1, blocks: 2, turnovers: 2, fieldGoalPct: 0.520, threePointPct: 0.500, freeThrowPct: 0.800, plusMinus: -8 },
          { playerId: 1629629, playerName: "Austin Reaves", position: "SG", jerseyNumber: "15", minutes: 33, points: 16, rebounds: 3, assists: 5, steals: 1, blocks: 0, turnovers: 2, fieldGoalPct: 0.400, threePointPct: 0.333, freeThrowPct: 0.750, plusMinus: -14 },
        ],
      },
      {
        game: { id: "0022400543", date: "2025-10-22", status: "Final", homeTeamId: LAL, visitorTeamId: BOS, homeScore: 120, visitorScore: 114, arena: "Crypto.com Arena" },
        homeTeam: team(LAL),
        visitorTeam: team(BOS),
        homePlayerStats: [
          { playerId: 2544, playerName: "LeBron James", position: "SF", jerseyNumber: "23", minutes: 36, points: 32, rebounds: 9, assists: 10, steals: 2, blocks: 0, turnovers: 3, fieldGoalPct: 0.524, threePointPct: 0.375, freeThrowPct: 0.833, plusMinus: 9 },
          { playerId: 203076, playerName: "Anthony Davis", position: "PF", jerseyNumber: "3", minutes: 38, points: 34, rebounds: 11, assists: 3, steals: 2, blocks: 3, turnovers: 1, fieldGoalPct: 0.550, threePointPct: 0.000, freeThrowPct: 0.900, plusMinus: 8 },
          { playerId: 1629629, playerName: "Austin Reaves", position: "SG", jerseyNumber: "15", minutes: 35, points: 24, rebounds: 4, assists: 8, steals: 1, blocks: 0, turnovers: 2, fieldGoalPct: 0.500, threePointPct: 0.429, freeThrowPct: 1.000, plusMinus: 6 },
        ],
        visitorPlayerStats: [
          { playerId: 1628369, playerName: "Jayson Tatum", position: "SF", jerseyNumber: "0", minutes: 38, points: 28, rebounds: 6, assists: 8, steals: 1, blocks: 0, turnovers: 5, fieldGoalPct: 0.440, threePointPct: 0.333, freeThrowPct: 0.875, plusMinus: -4 },
          { playerId: 1628370, playerName: "Jaylen Brown", position: "SG", jerseyNumber: "7", minutes: 36, points: 30, rebounds: 5, assists: 2, steals: 0, blocks: 1, turnovers: 3, fieldGoalPct: 0.520, threePointPct: 0.444, freeThrowPct: 0.750, plusMinus: -2 },
          { playerId: 203935, playerName: "Jrue Holiday", position: "PG", jerseyNumber: "4", minutes: 34, points: 16, rebounds: 5, assists: 6, steals: 1, blocks: 1, turnovers: 2, fieldGoalPct: 0.400, threePointPct: 0.286, freeThrowPct: 1.000, plusMinus: -7 },
        ],
      },
    ],
  },

  // Knicks vs Warriors
  [key(NYK, GSW)]: {
    team: team(NYK),
    opponent: team(GSW),
    teamWins: 2,
    opponentWins: 3,
    games: [
      {
        game: { id: "0022500400", date: "2026-02-01", status: "Final", homeTeamId: GSW, visitorTeamId: NYK, homeScore: 121, visitorScore: 115, arena: "Chase Center" },
        homeTeam: team(GSW),
        visitorTeam: team(NYK),
        homePlayerStats: [
          { playerId: 201939, playerName: "Stephen Curry", position: "PG", jerseyNumber: "30", minutes: 36, points: 36, rebounds: 5, assists: 8, steals: 2, blocks: 0, turnovers: 3, fieldGoalPct: 0.520, threePointPct: 0.545, freeThrowPct: 0.900, plusMinus: 10 },
        ],
        visitorPlayerStats: [
          { playerId: 203944, playerName: "Jalen Brunson", position: "PG", jerseyNumber: "11", minutes: 38, points: 32, rebounds: 3, assists: 9, steals: 1, blocks: 0, turnovers: 4, fieldGoalPct: 0.480, threePointPct: 0.400, freeThrowPct: 0.923, plusMinus: -4 },
        ],
      },
      {
        game: { id: "0022500280", date: "2026-01-12", status: "Final", homeTeamId: NYK, visitorTeamId: GSW, homeScore: 118, visitorScore: 105, arena: "Madison Square Garden" },
        homeTeam: team(NYK),
        visitorTeam: team(GSW),
        homePlayerStats: [
          { playerId: 203944, playerName: "Jalen Brunson", position: "PG", jerseyNumber: "11", minutes: 36, points: 35, rebounds: 4, assists: 11, steals: 2, blocks: 0, turnovers: 2, fieldGoalPct: 0.550, threePointPct: 0.500, freeThrowPct: 0.857, plusMinus: 15 },
        ],
        visitorPlayerStats: [
          { playerId: 201939, playerName: "Stephen Curry", position: "PG", jerseyNumber: "30", minutes: 35, points: 28, rebounds: 4, assists: 6, steals: 1, blocks: 0, turnovers: 5, fieldGoalPct: 0.440, threePointPct: 0.375, freeThrowPct: 1.000, plusMinus: -12 },
        ],
      },
      {
        game: { id: "0022400900", date: "2025-12-20", status: "Final", homeTeamId: GSW, visitorTeamId: NYK, homeScore: 112, visitorScore: 108, arena: "Chase Center" },
        homeTeam: team(GSW),
        visitorTeam: team(NYK),
        homePlayerStats: [
          { playerId: 201939, playerName: "Stephen Curry", position: "PG", jerseyNumber: "30", minutes: 37, points: 30, rebounds: 6, assists: 7, steals: 1, blocks: 0, turnovers: 2, fieldGoalPct: 0.480, threePointPct: 0.462, freeThrowPct: 0.875, plusMinus: 6 },
        ],
        visitorPlayerStats: [
          { playerId: 203944, playerName: "Jalen Brunson", position: "PG", jerseyNumber: "11", minutes: 38, points: 28, rebounds: 5, assists: 8, steals: 0, blocks: 0, turnovers: 3, fieldGoalPct: 0.458, threePointPct: 0.333, freeThrowPct: 0.900, plusMinus: -3 },
        ],
      },
      {
        game: { id: "0022400680", date: "2025-11-05", status: "Final", homeTeamId: NYK, visitorTeamId: GSW, homeScore: 125, visitorScore: 119, arena: "Madison Square Garden" },
        homeTeam: team(NYK),
        visitorTeam: team(GSW),
        homePlayerStats: [
          { playerId: 203944, playerName: "Jalen Brunson", position: "PG", jerseyNumber: "11", minutes: 39, points: 38, rebounds: 3, assists: 10, steals: 1, blocks: 0, turnovers: 2, fieldGoalPct: 0.560, threePointPct: 0.500, freeThrowPct: 0.933, plusMinus: 8 },
        ],
        visitorPlayerStats: [
          { playerId: 201939, playerName: "Stephen Curry", position: "PG", jerseyNumber: "30", minutes: 38, points: 34, rebounds: 5, assists: 9, steals: 2, blocks: 0, turnovers: 4, fieldGoalPct: 0.500, threePointPct: 0.429, freeThrowPct: 1.000, plusMinus: -4 },
        ],
      },
      {
        game: { id: "0022400510", date: "2025-10-28", status: "Final", homeTeamId: GSW, visitorTeamId: NYK, homeScore: 116, visitorScore: 110, arena: "Chase Center" },
        homeTeam: team(GSW),
        visitorTeam: team(NYK),
        homePlayerStats: [
          { playerId: 201939, playerName: "Stephen Curry", position: "PG", jerseyNumber: "30", minutes: 36, points: 32, rebounds: 4, assists: 7, steals: 1, blocks: 1, turnovers: 2, fieldGoalPct: 0.500, threePointPct: 0.500, freeThrowPct: 0.857, plusMinus: 8 },
        ],
        visitorPlayerStats: [
          { playerId: 203944, playerName: "Jalen Brunson", position: "PG", jerseyNumber: "11", minutes: 37, points: 26, rebounds: 4, assists: 7, steals: 0, blocks: 0, turnovers: 3, fieldGoalPct: 0.440, threePointPct: 0.286, freeThrowPct: 0.889, plusMinus: -6 },
        ],
      },
    ],
  },
}

export const MOCK_SEASON_AVERAGES: Record<number, PlayerSeasonAvg> = {
  // Celtics
  1628369: { playerId: 1628369, playerName: "Jayson Tatum", position: "SF", jerseyNumber: "0", gamesPlayed: 58, ptsAvg: 27.1, rebAvg: 8.4, astAvg: 4.9, stlAvg: 1.1, blkAvg: 0.6, fgPct: 0.464, fg3Pct: 0.378, tsPct: 0.594 },
  1628370: { playerId: 1628370, playerName: "Jaylen Brown", position: "SG", jerseyNumber: "7", gamesPlayed: 55, ptsAvg: 23.5, rebAvg: 5.2, astAvg: 3.4, stlAvg: 1.2, blkAvg: 0.5, fgPct: 0.462, fg3Pct: 0.362, tsPct: 0.576 },
  203935: { playerId: 203935, playerName: "Jrue Holiday", position: "PG", jerseyNumber: "4", gamesPlayed: 60, ptsAvg: 14.2, rebAvg: 4.5, astAvg: 7.1, stlAvg: 1.3, blkAvg: 0.4, fgPct: 0.428, fg3Pct: 0.352, tsPct: 0.558 },
  // Lakers
  2544: { playerId: 2544, playerName: "LeBron James", position: "SF", jerseyNumber: "23", gamesPlayed: 52, ptsAvg: 25.8, rebAvg: 7.2, astAvg: 8.1, stlAvg: 1.2, blkAvg: 0.6, fgPct: 0.510, fg3Pct: 0.358, tsPct: 0.605 },
  203076: { playerId: 203076, playerName: "Anthony Davis", position: "PF", jerseyNumber: "3", gamesPlayed: 55, ptsAvg: 26.4, rebAvg: 11.8, astAvg: 2.8, stlAvg: 1.4, blkAvg: 2.2, fgPct: 0.536, fg3Pct: 0.242, tsPct: 0.612 },
  1629629: { playerId: 1629629, playerName: "Austin Reaves", position: "SG", jerseyNumber: "15", gamesPlayed: 58, ptsAvg: 17.4, rebAvg: 3.8, astAvg: 5.8, stlAvg: 0.9, blkAvg: 0.3, fgPct: 0.438, fg3Pct: 0.368, tsPct: 0.572 },
  // Warriors
  201939: { playerId: 201939, playerName: "Stephen Curry", position: "PG", jerseyNumber: "30", gamesPlayed: 56, ptsAvg: 26.8, rebAvg: 4.8, astAvg: 6.2, stlAvg: 1.0, blkAvg: 0.3, fgPct: 0.468, fg3Pct: 0.408, tsPct: 0.628 },
  // Knicks
  203944: { playerId: 203944, playerName: "Jalen Brunson", position: "PG", jerseyNumber: "11", gamesPlayed: 57, ptsAvg: 25.2, rebAvg: 3.6, astAvg: 7.8, stlAvg: 0.8, blkAvg: 0.2, fgPct: 0.472, fg3Pct: 0.382, tsPct: 0.596 },
}
