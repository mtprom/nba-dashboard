import { HistoryGame, HistoryFilter, HistoryMetrics, SeasonBarDatum, HeatmapCell } from "@/types/history"

// Months shown in heatmap (Oct–Apr)
export const HEATMAP_MONTHS = [10, 11, 12, 1, 2, 3, 4]
export const HEATMAP_MONTH_LABELS: Record<number, string> = {
  10: "Oct", 11: "Nov", 12: "Dec", 1: "Jan", 2: "Feb", 3: "Mar", 4: "Apr"
}

const SEASON_ANNOTATIONS: Record<number, string> = {
  1998: "lockout",
  2011: "lockout",
  2019: "bubble",
}

export const MOCK_GAMES: HistoryGame[] = [
  // ── 1994-95 Bulls dynasty ──
  { id: "g001", date: "1994-11-18", seasonYear: 1994, month: 11, homeTeamId: 1610612741, awayTeamId: 1610612747, homeScore: 117, awayScore: 95  }, // CHI def LAL +22 (blowout)
  { id: "g002", date: "1995-03-12", seasonYear: 1994, month: 3,  homeTeamId: 1610612738, awayTeamId: 1610612741, homeScore: 99,  awayScore: 98  }, // BOS def CHI +1 (closest)

  // ── 1996-97 Jordan peak ──
  { id: "g003", date: "1996-12-04", seasonYear: 1996, month: 12, homeTeamId: 1610612741, awayTeamId: 1610612759, homeScore: 103, awayScore: 83  }, // CHI def SAS +20 (blowout)
  { id: "g004", date: "1997-02-20", seasonYear: 1996, month: 2,  homeTeamId: 1610612747, awayTeamId: 1610612759, homeScore: 112, awayScore: 110 }, // LAL def SAS +2 (closest)

  // ── 1998-99 Lockout season (Feb–May, seasonYear: 1998) ──
  { id: "g005", date: "1999-02-06", seasonYear: 1998, month: 2,  homeTeamId: 1610612759, awayTeamId: 1610612748, homeScore: 88,  awayScore: 87  }, // SAS def MIA +1 (closest)
  { id: "g006", date: "1999-03-21", seasonYear: 1998, month: 3,  homeTeamId: 1610612759, awayTeamId: 1610612747, homeScore: 74,  awayScore: 96  }, // LAL def SAS +22 (blowout)
  { id: "g007", date: "1999-04-30", seasonYear: 1998, month: 4,  homeTeamId: 1610612748, awayTeamId: 1610612739, homeScore: 95,  awayScore: 93  }, // MIA def CLE +2 (closest)

  // ── 2000-01 Shaq/Kobe ──
  { id: "g008", date: "2000-11-07", seasonYear: 2000, month: 11, homeTeamId: 1610612747, awayTeamId: 1610612755, homeScore: 108, awayScore: 85  }, // LAL def PHI +23 (blowout)
  { id: "g009", date: "2001-02-14", seasonYear: 2000, month: 2,  homeTeamId: 1610612755, awayTeamId: 1610612747, homeScore: 107, awayScore: 101 }, // PHI def LAL +6

  // ── 2003-04 Pistons ──
  { id: "g010", date: "2003-11-22", seasonYear: 2003, month: 11, homeTeamId: 1610612765, awayTeamId: 1610612747, homeScore: 97,  awayScore: 91  }, // DET def LAL +6
  { id: "g011", date: "2004-03-05", seasonYear: 2003, month: 3,  homeTeamId: 1610612747, awayTeamId: 1610612765, homeScore: 88,  awayScore: 115 }, // DET def LAL +27 (blowout)

  // ── 2007-08 Celtics Big 3 ──
  { id: "g012", date: "2007-11-30", seasonYear: 2007, month: 11, homeTeamId: 1610612738, awayTeamId: 1610612747, homeScore: 107, awayScore: 94  }, // BOS def LAL +13
  { id: "g013", date: "2008-02-07", seasonYear: 2007, month: 2,  homeTeamId: 1610612747, awayTeamId: 1610612738, homeScore: 103, awayScore: 105 }, // BOS def LAL +2 (closest)

  // ── 2009-10 LeBron era ──
  { id: "g014", date: "2009-12-03", seasonYear: 2009, month: 12, homeTeamId: 1610612739, awayTeamId: 1610612744, homeScore: 124, awayScore: 99  }, // CLE def GSW +25 (blowout)
  { id: "g015", date: "2010-03-29", seasonYear: 2009, month: 3,  homeTeamId: 1610612744, awayTeamId: 1610612739, homeScore: 119, awayScore: 118 }, // GSW def CLE +1 (closest)

  // ── 2011-12 Lockout season (Dec–Apr, seasonYear: 2011) ──
  { id: "g016", date: "2011-12-29", seasonYear: 2011, month: 12, homeTeamId: 1610612738, awayTeamId: 1610612761, homeScore: 102, awayScore: 101 }, // BOS def TOR +1 (closest)
  { id: "g017", date: "2012-01-25", seasonYear: 2011, month: 1,  homeTeamId: 1610612749, awayTeamId: 1610612741, homeScore: 113, awayScore: 91  }, // MIL def CHI +22 (blowout)
  { id: "g018", date: "2012-03-11", seasonYear: 2011, month: 3,  homeTeamId: 1610612748, awayTeamId: 1610612744, homeScore: 103, awayScore: 101 }, // MIA def GSW +2 (closest)

  // ── 2012-13 Heat dynasty ──
  { id: "g019", date: "2012-11-09", seasonYear: 2012, month: 11, homeTeamId: 1610612748, awayTeamId: 1610612759, homeScore: 120, awayScore: 98  }, // MIA def SAS +22 (blowout)
  { id: "g020", date: "2013-04-14", seasonYear: 2012, month: 4,  homeTeamId: 1610612759, awayTeamId: 1610612748, homeScore: 103, awayScore: 88  }, // SAS def MIA +15

  // ── 2015-16 Warriors 73-win season ──
  { id: "g021", date: "2015-10-27", seasonYear: 2015, month: 10, homeTeamId: 1610612744, awayTeamId: 1610612765, homeScore: 111, awayScore: 95  }, // GSW def DET +16
  { id: "g022", date: "2016-02-27", seasonYear: 2015, month: 2,  homeTeamId: 1610612741, awayTeamId: 1610612744, homeScore: 113, awayScore: 120 }, // GSW def CHI +7

  // ── 2016-17 ──
  { id: "g023", date: "2016-11-16", seasonYear: 2016, month: 11, homeTeamId: 1610612744, awayTeamId: 1610612739, homeScore: 127, awayScore: 104 }, // GSW def CLE +23 (blowout)
  { id: "g024", date: "2017-01-16", seasonYear: 2016, month: 1,  homeTeamId: 1610612739, awayTeamId: 1610612744, homeScore: 116, awayScore: 105 }, // CLE def GSW +11

  // ── 2018-19 Kawhi Raptors ──
  { id: "g025", date: "2018-11-29", seasonYear: 2018, month: 11, homeTeamId: 1610612761, awayTeamId: 1610612744, homeScore: 131, awayScore: 128 }, // TOR def GSW +3 (closest)
  { id: "g026", date: "2019-03-05", seasonYear: 2018, month: 3,  homeTeamId: 1610612744, awayTeamId: 1610612761, homeScore: 127, awayScore: 125 }, // GSW def TOR +2 (closest)

  // ── 2019-20 Bubble (Jul–Oct, seasonYear: 2019) ──
  { id: "g027", date: "2020-08-03", seasonYear: 2019, month: 8,  homeTeamId: 1610612747, awayTeamId: 1610612744, homeScore: 124, awayScore: 121 }, // LAL def GSW +3 (closest, bubble)
  { id: "g028", date: "2020-08-13", seasonYear: 2019, month: 8,  homeTeamId: 1610612744, awayTeamId: 1610612760, homeScore: 119, awayScore: 93  }, // GSW def OKC +26 (blowout, bubble)
  { id: "g029", date: "2020-09-06", seasonYear: 2019, month: 9,  homeTeamId: 1610612748, awayTeamId: 1610612741, homeScore: 116, awayScore: 114 }, // MIA def CHI +2 (closest, bubble)

  // ── 2021-22 ──
  { id: "g030", date: "2021-10-21", seasonYear: 2021, month: 10, homeTeamId: 1610612747, awayTeamId: 1610612759, homeScore: 128, awayScore: 138 }, // SAS def LAL +10
  { id: "g031", date: "2022-01-20", seasonYear: 2021, month: 1,  homeTeamId: 1610612738, awayTeamId: 1610612755, homeScore: 135, awayScore: 87  }, // BOS def PHI +48 (blowout)
  { id: "g032", date: "2022-03-27", seasonYear: 2021, month: 3,  homeTeamId: 1610612755, awayTeamId: 1610612738, homeScore: 99,  awayScore: 97  }, // PHI def BOS +2 (closest)

  // ── 2022-23 ──
  { id: "g033", date: "2022-10-18", seasonYear: 2022, month: 10, homeTeamId: 1610612744, awayTeamId: 1610612747, homeScore: 123, awayScore: 109 }, // GSW def LAL +14
  { id: "g034", date: "2022-12-25", seasonYear: 2022, month: 12, homeTeamId: 1610612747, awayTeamId: 1610612752, homeScore: 119, awayScore: 112 }, // LAL def NYK +7
  { id: "g035", date: "2023-02-09", seasonYear: 2022, month: 2,  homeTeamId: 1610612752, awayTeamId: 1610612738, homeScore: 118, awayScore: 95  }, // NYK def BOS +23 (blowout)

  // ── 2023-24 ──
  { id: "g036", date: "2023-10-24", seasonYear: 2023, month: 10, homeTeamId: 1610612738, awayTeamId: 1610612755, homeScore: 108, awayScore: 82  }, // BOS def PHI +26 (blowout)
  { id: "g037", date: "2023-11-17", seasonYear: 2023, month: 11, homeTeamId: 1610612747, awayTeamId: 1610612744, homeScore: 110, awayScore: 113 }, // GSW def LAL +3 (closest)
  { id: "g038", date: "2023-12-25", seasonYear: 2023, month: 12, homeTeamId: 1610612752, awayTeamId: 1610612738, homeScore: 129, awayScore: 103 }, // NYK def BOS +26 (blowout)
  { id: "g039", date: "2024-01-28", seasonYear: 2023, month: 1,  homeTeamId: 1610612760, awayTeamId: 1610612759, homeScore: 118, awayScore: 116 }, // OKC def SAS +2 (closest)
  { id: "g040", date: "2024-03-22", seasonYear: 2023, month: 3,  homeTeamId: 1610612759, awayTeamId: 1610612760, homeScore: 109, awayScore: 107 }, // SAS def OKC +2 (closest)

  // ── 2024-25 (current season) ──
  { id: "g041", date: "2024-10-22", seasonYear: 2024, month: 10, homeTeamId: 1610612738, awayTeamId: 1610612752, homeScore: 132, awayScore: 109 }, // BOS def NYK +23 (blowout)
  { id: "g042", date: "2024-11-14", seasonYear: 2024, month: 11, homeTeamId: 1610612760, awayTeamId: 1610612744, homeScore: 129, awayScore: 104 }, // OKC def GSW +25 (blowout)
  { id: "g043", date: "2024-12-19", seasonYear: 2024, month: 12, homeTeamId: 1610612752, awayTeamId: 1610612760, homeScore: 114, awayScore: 113 }, // NYK def OKC +1 (closest)
  { id: "g044", date: "2025-01-11", seasonYear: 2024, month: 1,  homeTeamId: 1610612747, awayTeamId: 1610612738, homeScore: 120, awayScore: 119 }, // LAL def BOS +1 (closest)

  // Extra games for variety ──
  { id: "g045", date: "2005-11-04", seasonYear: 2005, month: 11, homeTeamId: 1610612756, awayTeamId: 1610612747, homeScore: 116, awayScore: 91  }, // PHX def LAL +25 (blowout)
  { id: "g046", date: "2006-02-19", seasonYear: 2005, month: 2,  homeTeamId: 1610612747, awayTeamId: 1610612756, homeScore: 99,  awayScore: 100 }, // PHX def LAL +1 (closest)
  { id: "g047", date: "2013-12-16", seasonYear: 2013, month: 12, homeTeamId: 1610612759, awayTeamId: 1610612755, homeScore: 100, awayScore: 76  }, // SAS def PHI +24 (blowout)
  { id: "g048", date: "2014-02-08", seasonYear: 2013, month: 2,  homeTeamId: 1610612755, awayTeamId: 1610612748, homeScore: 98,  awayScore: 97  }, // PHI def MIA +1 (closest)
  { id: "g049", date: "2017-11-22", seasonYear: 2017, month: 11, homeTeamId: 1610612743, awayTeamId: 1610612762, homeScore: 126, awayScore: 107 }, // DEN def UTA +19
  { id: "g050", date: "2018-03-11", seasonYear: 2017, month: 3,  homeTeamId: 1610612762, awayTeamId: 1610612743, homeScore: 114, awayScore: 112 }, // UTA def DEN +2 (closest)
]

// ── Pure helpers ──────────────────────────────────────────────────

export function getMargin(game: HistoryGame): number {
  return Math.abs(game.homeScore - game.awayScore)
}

export function formatSeasonLabel(seasonYear: number): string {
  return `${seasonYear}-${String(seasonYear + 1).slice(2)}`
}

export function filterGames(games: HistoryGame[], filter: HistoryFilter): HistoryGame[] {
  return games.filter((g) => {
    if (filter.teamId !== null && g.homeTeamId !== filter.teamId && g.awayTeamId !== filter.teamId) {
      return false
    }
    return g.seasonYear >= filter.fromSeason && g.seasonYear <= filter.toSeason
  })
}

export function computeMetrics(games: HistoryGame[]): HistoryMetrics {
  if (games.length === 0) {
    return { totalGames: 0, seasonsCovered: 0, avgMarginOfVictory: 0 }
  }
  const seasons = new Set(games.map((g) => g.seasonYear))
  const totalMargin = games.reduce((sum, g) => sum + getMargin(g), 0)
  return {
    totalGames: games.length,
    seasonsCovered: seasons.size,
    avgMarginOfVictory: totalMargin / games.length,
  }
}

export function buildSeasonBarData(
  games: HistoryGame[],
  fromSeason: number,
  toSeason: number
): SeasonBarDatum[] {
  const countsBySeason: Record<number, number> = {}
  for (const g of games) {
    countsBySeason[g.seasonYear] = (countsBySeason[g.seasonYear] ?? 0) + 1
  }
  const result: SeasonBarDatum[] = []
  for (let y = fromSeason; y <= toSeason; y++) {
    result.push({
      seasonYear: y,
      seasonLabel: formatSeasonLabel(y),
      gameCount: countsBySeason[y] ?? 0,
      annotation: SEASON_ANNOTATIONS[y],
    })
  }
  return result
}

export function buildHeatmapData(games: HistoryGame[]): HeatmapCell[] {
  // Only include games in Oct–Apr (months 10,11,12,1,2,3,4)
  const heatMonthSet = new Set(HEATMAP_MONTHS)

  // Aggregate wins/losses per (seasonYear, month)
  const map: Record<string, { wins: number; losses: number }> = {}
  for (const g of games) {
    if (!heatMonthSet.has(g.month)) continue
    const key = `${g.seasonYear}:${g.month}`
    if (!map[key]) map[key] = { wins: 0, losses: 0 }
    const homeWon = g.homeScore > g.awayScore
    if (homeWon) map[key].wins++
    else map[key].losses++
  }

  const cells: HeatmapCell[] = []
  const seasons = Array.from(new Set(games.map((g) => g.seasonYear))).sort()
  for (const sy of seasons) {
    for (const mo of HEATMAP_MONTHS) {
      const key = `${sy}:${mo}`
      const entry = map[key]
      if (entry) {
        const total = entry.wins + entry.losses
        cells.push({ seasonYear: sy, month: mo, wins: entry.wins, losses: entry.losses, winPct: total > 0 ? entry.wins / total : NaN })
      } else {
        cells.push({ seasonYear: sy, month: mo, wins: 0, losses: 0, winPct: NaN })
      }
    }
  }
  return cells
}

export function getClosestGames(games: HistoryGame[]): HistoryGame[] {
  return [...games].sort((a, b) => getMargin(a) - getMargin(b))
}

export function getBlowouts(games: HistoryGame[]): HistoryGame[] {
  return [...games]
    .filter((g) => getMargin(g) >= 20)
    .sort((a, b) => getMargin(b) - getMargin(a))
}
