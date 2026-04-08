import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card"
import { SeasonStatDatum } from "@/types/history"

interface SeasonIdentityTagsProps {
  data: SeasonStatDatum[]
  teamColor: string
}

type TagName =
  | "Dominant"
  | "Strong"
  | "Balanced"
  | "Struggling"
  | "Rebuilding"
  | "Blowout Heavy"
  | "Close Games"

function getSeasonTag(d: SeasonStatDatum): TagName {
  const { winPct, closeGames, blowoutGames, gameCount } = d
  if (winPct !== null) {
    if (winPct >= 0.65) return "Dominant"
    if (winPct >= 0.55) return "Strong"
    if (winPct <= 0.35) return "Rebuilding"
    if (winPct <= 0.45) return "Struggling"
  }
  if (gameCount > 0) {
    if (blowoutGames / gameCount >= 0.30) return "Blowout Heavy"
    if (closeGames / gameCount >= 0.35) return "Close Games"
  }
  return "Balanced"
}

// Hex → rgba
function rgba(hex: string, alpha: number): string {
  const r = parseInt(hex.slice(1, 3), 16)
  const g = parseInt(hex.slice(3, 5), 16)
  const b = parseInt(hex.slice(5, 7), 16)
  return `rgba(${r},${g},${b},${alpha})`
}

function tagStyle(tag: TagName, teamColor: string): React.CSSProperties {
  const isHex = teamColor.startsWith("#")
  switch (tag) {
    case "Dominant":
      return {
        backgroundColor: isHex ? rgba(teamColor, 0.95) : teamColor,
        color: "#fff",
        border: "none",
      }
    case "Strong":
      return {
        backgroundColor: isHex ? rgba(teamColor, 0.7) : teamColor,
        color: "#fff",
        border: "none",
      }
    case "Blowout Heavy":
      return {
        backgroundColor: isHex ? rgba(teamColor, 0.55) : teamColor,
        color: "#fff",
        border: "none",
      }
    case "Close Games":
      return {
        backgroundColor: isHex ? rgba(teamColor, 0.4) : teamColor,
        color: "#fff",
        border: "none",
      }
    case "Struggling":
      return {
        backgroundColor: "rgba(245, 158, 11, 0.18)",
        color: "rgb(217, 119, 6)",
        border: "1px solid rgba(245, 158, 11, 0.35)",
      }
    case "Rebuilding":
      return {
        backgroundColor: "rgba(239, 68, 68, 0.12)",
        color: "rgb(220, 38, 38)",
        border: "1px solid rgba(239, 68, 68, 0.3)",
      }
    case "Balanced":
    default:
      return {
        backgroundColor: "hsl(var(--muted))",
        color: "hsl(var(--muted-foreground))",
        border: "1px solid hsl(var(--border))",
      }
  }
}

export default function SeasonIdentityTags({ data, teamColor }: SeasonIdentityTagsProps) {
  const tagged = data.filter((d) => d.gameCount > 0)

  return (
    <Card>
      <CardHeader className="pb-2">
        <CardTitle className="text-base font-medium">Season Identity</CardTitle>
      </CardHeader>
      <CardContent className="px-6 pb-6">
        <div className="flex flex-wrap gap-x-3 gap-y-4">
          {tagged.map((d) => {
            const tag = getSeasonTag(d)
            const style = tagStyle(tag, teamColor)
            return (
              <div key={d.seasonYear} className="flex min-w-[72px] flex-col items-center gap-1">
                <span className="text-[10px] text-muted-foreground">{d.seasonLabel}</span>
                <span
                  className="rounded-full px-2 py-0.5 text-[10px] font-semibold whitespace-nowrap"
                  style={style}
                >
                  {tag}
                </span>
              </div>
            )
          })}
        </div>
      </CardContent>
    </Card>
  )
}
