import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card"
import { ScrollArea } from "@/components/ui/scroll-area"
import { HeatmapCell } from "@/types/history"
import { HEATMAP_MONTHS, HEATMAP_MONTH_LABELS, formatSeasonLabel } from "@/data/mock-history"

interface MonthlyHeatmapProps {
  data: HeatmapCell[]
  seasonYears: number[]  // all seasons to show as rows, sorted asc
  teamColor: string
}

function hexToRgb(hex: string): { r: number; g: number; b: number } | null {
  if (!hex.startsWith("#") || hex.length !== 7) return null
  return {
    r: parseInt(hex.slice(1, 3), 16),
    g: parseInt(hex.slice(3, 5), 16),
    b: parseInt(hex.slice(5, 7), 16),
  }
}

function mixRgb(
  a: { r: number; g: number; b: number },
  b: { r: number; g: number; b: number },
  t: number
): string {
  const clamped = Math.max(0, Math.min(1, t))
  const r = Math.round(a.r + (b.r - a.r) * clamped)
  const g = Math.round(a.g + (b.g - a.g) * clamped)
  const bVal = Math.round(a.b + (b.b - a.b) * clamped)
  return `rgb(${r},${g},${bVal})`
}

function winPctToColor(winPct: number | null, teamColor: string): string {
  if (winPct === null || isNaN(winPct)) return "hsl(var(--muted))"

  const clamped = Math.max(0, Math.min(1, winPct))
  const teamRgb = hexToRgb(teamColor)

  if (!teamRgb) return "hsl(var(--muted))"

  const lowRgb = { r: 94, g: 100, b: 112 }
  return mixRgb(lowRgb, teamRgb, clamped)
}

function cellTitle(seasonYear: number, month: number, cell: HeatmapCell | undefined): string {
  const seasonLabel = formatSeasonLabel(seasonYear)
  const monthLabel = HEATMAP_MONTH_LABELS[month]
  if (!cell || cell.winPct === null) return `${seasonLabel} · ${monthLabel}\nNo data`
  const pct = (cell.winPct * 100).toFixed(1)
  return `${seasonLabel} · ${monthLabel}\n${cell.wins}W–${cell.losses}L · ${pct}%`
}

export default function MonthlyHeatmap({ data, seasonYears, teamColor }: MonthlyHeatmapProps) {
  // Build lookup: `${seasonYear}:${month}` → HeatmapCell
  const lookup = new Map<string, HeatmapCell>()
  for (const cell of data) {
    lookup.set(`${cell.seasonYear}:${cell.month}`, cell)
  }

  // Seasons descending (most recent at top)
  const seasons = [...seasonYears].sort((a, b) => b - a)

  return (
    <Card className="flex flex-col">
      <CardHeader className="pb-2">
        <CardTitle className="text-base font-medium">Monthly Win % Heatmap</CardTitle>
      </CardHeader>
      <CardContent className="pb-4">
        <ScrollArea className="h-[480px]">
          <div className="min-w-[340px]">
            {/* Month header row */}
            <div className="grid gap-[2px]" style={{ gridTemplateColumns: "64px repeat(7, 1fr)" }}>
              <div /> {/* spacer */}
              {HEATMAP_MONTHS.map((mo) => (
                <div
                  key={mo}
                  className="text-center text-[10px] text-muted-foreground font-medium pb-1"
                >
                  {HEATMAP_MONTH_LABELS[mo]}
                </div>
              ))}

              {/* Data rows */}
              {seasons.map((sy) => (
                <>
                  {/* Season label */}
                  <div
                    key={`label-${sy}`}
                    className="flex items-center text-[10px] text-muted-foreground pr-1 leading-none"
                  >
                    {formatSeasonLabel(sy)}
                  </div>
                  {/* Month cells */}
                  {HEATMAP_MONTHS.map((mo) => {
                    const cell = lookup.get(`${sy}:${mo}`)
                    const color = winPctToColor(cell?.winPct ?? null, teamColor)
                    const title = cellTitle(sy, mo, cell)
                    return (
                      <div
                        key={`${sy}-${mo}`}
                        title={title}
                        className="rounded-sm hover:opacity-75 transition-opacity cursor-default"
                        style={{ backgroundColor: color, height: "22px" }}
                      />
                    )
                  })}
                </>
              ))}
            </div>
          </div>
        </ScrollArea>

        {/* Legend */}
        <div className="mt-4 flex items-center gap-2">
          <span className="text-[10px] text-muted-foreground">0%</span>
          <div
            className="h-2.5 flex-1 rounded-sm"
            style={{
              background: `linear-gradient(to right, rgb(94,100,112), ${teamColor})`,
            }}
          />
          <span className="text-[10px] text-muted-foreground">100%</span>
        </div>
        <p className="mt-1 text-[10px] text-muted-foreground">
          Hover cells for details · Gray = no data · Team color ramp = low to high win rate
        </p>
      </CardContent>
    </Card>
  )
}
