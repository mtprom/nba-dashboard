export interface TeamColorDef {
  primary: string
  /** Explicit override for dark backgrounds. When omitted, getTeamColors falls back
   *  to the luminance check before using primary. */
  primaryDark?: string
  secondary: string
}

export const TEAM_COLORS: Record<number, TeamColorDef> = {
  // Eastern Conference
  1610612737: { primary: "#E03A3E", secondary: "#C1D32F" },  // Atlanta Hawks
  1610612738: { primary: "#007A33", secondary: "#BA9653" },  // Boston Celtics
  // Nets: pure black primary is invisible on dark bg — use silver instead of stark white
  1610612751: { primary: "#000000", primaryDark: "#C6CDD3", secondary: "#FFFFFF" },  // Brooklyn Nets
  // Hornets: near-black navy primary — teal reads much better
  1610612766: { primary: "#1D1160", primaryDark: "#00788C", secondary: "#00788C" },  // Charlotte Hornets
  1610612741: { primary: "#CE1141", secondary: "#000000" },  // Chicago Bulls
  1610612739: { primary: "#860038", secondary: "#FDBB30" },  // Cleveland Cavaliers
  1610612765: { primary: "#C8102E", secondary: "#1D42BA" },  // Detroit Pistons
  // Pacers: dark navy primary — gold is the right brand color for dark bg
  1610612754: { primary: "#002D62", primaryDark: "#FDBB30", secondary: "#FDBB30" },  // Indiana Pacers
  1610612748: { primary: "#98002E", secondary: "#F9A01B" },  // Miami Heat
  1610612749: { primary: "#00471B", secondary: "#EEE1C6" },  // Milwaukee Bucks
  1610612752: { primary: "#006BB6", secondary: "#F58426" },  // New York Knicks
  1610612753: { primary: "#0077C0", secondary: "#C4CED4" },  // Orlando Magic
  1610612755: { primary: "#006BB6", secondary: "#ED174C" },  // Philadelphia 76ers
  1610612761: { primary: "#CE1141", secondary: "#000000" },  // Toronto Raptors
  1610612764: { primary: "#002B5C", secondary: "#E31837" },  // Washington Wizards

  // Western Conference
  1610612742: { primary: "#00538C", secondary: "#B8C4CA" },  // Dallas Mavericks
  1610612743: { primary: "#0E2240", secondary: "#FEC524" },  // Denver Nuggets
  1610612744: { primary: "#1D428A", secondary: "#FFC72C" },  // Golden State Warriors
  1610612745: { primary: "#CE1141", secondary: "#C4CED4" },  // Houston Rockets
  1610612746: { primary: "#C8102E", secondary: "#1D428A" },  // LA Clippers
  1610612747: { primary: "#552583", secondary: "#FDB927" },  // Los Angeles Lakers
  1610612763: { primary: "#5D76A9", secondary: "#12173F" },  // Memphis Grizzlies
  1610612750: { primary: "#0C2340", secondary: "#236192" },  // Minnesota Timberwolves
  1610612740: { primary: "#0C2340", secondary: "#C8102E" },  // New Orleans Pelicans
  1610612760: { primary: "#007AC1", secondary: "#EF3B24" },  // Oklahoma City Thunder
  // Suns: near-black navy primary — orange is their signature dark-bg color
  1610612756: { primary: "#1D1160", primaryDark: "#E56020", secondary: "#E56020" },  // Phoenix Suns
  1610612757: { primary: "#E03A3E", secondary: "#000000" },  // Portland Trail Blazers
  1610612758: { primary: "#5A2D81", secondary: "#63727A" },  // Sacramento Kings
  1610612759: { primary: "#C4CED4", secondary: "#000000" },  // San Antonio Spurs
  1610612762: { primary: "#002B5C", secondary: "#F9A01B" },  // Utah Jazz
}

function relativeLuminance(hex: string): number {
  const r = parseInt(hex.slice(1, 3), 16) / 255
  const g = parseInt(hex.slice(3, 5), 16) / 255
  const b = parseInt(hex.slice(5, 7), 16) / 255
  const toLinear = (c: number) =>
    c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4
  return 0.2126 * toLinear(r) + 0.7152 * toLinear(g) + 0.0722 * toLinear(b)
}

export function getTeamColors(teamId: number): { primary: string; secondary: string } {
  const colors = TEAM_COLORS[teamId] ?? { primary: "#6b7280", secondary: "#9ca3af" }

  // Strategy 1: use explicit dark-bg override if one is defined
  if (colors.primaryDark) {
    return { primary: colors.primaryDark, secondary: colors.secondary }
  }

  // Strategy 2: if primary is too dark to read on a dark background, fall back to secondary
  if (relativeLuminance(colors.primary) < 0.05) {
    return { primary: colors.secondary, secondary: colors.primary }
  }

  return { primary: colors.primary, secondary: colors.secondary }
}

export const TEAM_INFO: Record<number, { name: string; fullName: string; abbreviation: string; city: string; conference: string; division: string }> = {
  1610612737: { name: "Hawks", fullName: "Atlanta Hawks", abbreviation: "ATL", city: "Atlanta", conference: "East", division: "Southeast" },
  1610612738: { name: "Celtics", fullName: "Boston Celtics", abbreviation: "BOS", city: "Boston", conference: "East", division: "Atlantic" },
  1610612751: { name: "Nets", fullName: "Brooklyn Nets", abbreviation: "BKN", city: "Brooklyn", conference: "East", division: "Atlantic" },
  1610612766: { name: "Hornets", fullName: "Charlotte Hornets", abbreviation: "CHA", city: "Charlotte", conference: "East", division: "Southeast" },
  1610612741: { name: "Bulls", fullName: "Chicago Bulls", abbreviation: "CHI", city: "Chicago", conference: "East", division: "Central" },
  1610612739: { name: "Cavaliers", fullName: "Cleveland Cavaliers", abbreviation: "CLE", city: "Cleveland", conference: "East", division: "Central" },
  1610612765: { name: "Pistons", fullName: "Detroit Pistons", abbreviation: "DET", city: "Detroit", conference: "East", division: "Central" },
  1610612754: { name: "Pacers", fullName: "Indiana Pacers", abbreviation: "IND", city: "Indianapolis", conference: "East", division: "Central" },
  1610612748: { name: "Heat", fullName: "Miami Heat", abbreviation: "MIA", city: "Miami", conference: "East", division: "Southeast" },
  1610612749: { name: "Bucks", fullName: "Milwaukee Bucks", abbreviation: "MIL", city: "Milwaukee", conference: "East", division: "Central" },
  1610612752: { name: "Knicks", fullName: "New York Knicks", abbreviation: "NYK", city: "New York", conference: "East", division: "Atlantic" },
  1610612753: { name: "Magic", fullName: "Orlando Magic", abbreviation: "ORL", city: "Orlando", conference: "East", division: "Southeast" },
  1610612755: { name: "76ers", fullName: "Philadelphia 76ers", abbreviation: "PHI", city: "Philadelphia", conference: "East", division: "Atlantic" },
  1610612761: { name: "Raptors", fullName: "Toronto Raptors", abbreviation: "TOR", city: "Toronto", conference: "East", division: "Atlantic" },
  1610612764: { name: "Wizards", fullName: "Washington Wizards", abbreviation: "WAS", city: "Washington", conference: "East", division: "Southeast" },
  1610612742: { name: "Mavericks", fullName: "Dallas Mavericks", abbreviation: "DAL", city: "Dallas", conference: "West", division: "Southwest" },
  1610612743: { name: "Nuggets", fullName: "Denver Nuggets", abbreviation: "DEN", city: "Denver", conference: "West", division: "Northwest" },
  1610612744: { name: "Warriors", fullName: "Golden State Warriors", abbreviation: "GSW", city: "San Francisco", conference: "West", division: "Pacific" },
  1610612745: { name: "Rockets", fullName: "Houston Rockets", abbreviation: "HOU", city: "Houston", conference: "West", division: "Southwest" },
  1610612746: { name: "Clippers", fullName: "LA Clippers", abbreviation: "LAC", city: "Los Angeles", conference: "West", division: "Pacific" },
  1610612747: { name: "Lakers", fullName: "Los Angeles Lakers", abbreviation: "LAL", city: "Los Angeles", conference: "West", division: "Pacific" },
  1610612763: { name: "Grizzlies", fullName: "Memphis Grizzlies", abbreviation: "MEM", city: "Memphis", conference: "West", division: "Northwest" },
  1610612750: { name: "Timberwolves", fullName: "Minnesota Timberwolves", abbreviation: "MIN", city: "Minneapolis", conference: "West", division: "Northwest" },
  1610612740: { name: "Pelicans", fullName: "New Orleans Pelicans", abbreviation: "NOP", city: "New Orleans", conference: "West", division: "Southwest" },
  1610612760: { name: "Thunder", fullName: "Oklahoma City Thunder", abbreviation: "OKC", city: "Oklahoma City", conference: "West", division: "Northwest" },
  1610612756: { name: "Suns", fullName: "Phoenix Suns", abbreviation: "PHX", city: "Phoenix", conference: "West", division: "Pacific" },
  1610612757: { name: "Trail Blazers", fullName: "Portland Trail Blazers", abbreviation: "POR", city: "Portland", conference: "West", division: "Northwest" },
  1610612758: { name: "Kings", fullName: "Sacramento Kings", abbreviation: "SAC", city: "Sacramento", conference: "West", division: "Pacific" },
  1610612759: { name: "Spurs", fullName: "San Antonio Spurs", abbreviation: "SAS", city: "San Antonio", conference: "West", division: "Southwest" },
  1610612762: { name: "Jazz", fullName: "Utah Jazz", abbreviation: "UTA", city: "Salt Lake City", conference: "West", division: "Northwest" },
}
