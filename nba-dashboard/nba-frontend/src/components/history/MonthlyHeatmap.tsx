import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card"
import { ScrollArea } from "@/components/ui/scroll-area"
import { HeatmapCell } from "@/types/history"
import { HEATMAP_MONTHS, HEATMAP_MONTH_LABELS, formatSeasonLabel } from "@/data/mock-history"

interface MonthlyHeatmapProps {
  data: HeatmapCell[]
  seasonYears: number[]  // all seasons to show as rows, sorted asc
}

// Blue ramp: dark navy → blue-600 → blue-200
function winPctToColor(winPct: number): string {
  if (isNaN(winPct)) return "hsl(var(--muted))"

  const clamped = Math.max(0, Math.min(1, winPct))

  // Two-segment gradient
  let r: number, g: number, b: number
  if (clamped <= 0.5) {
    // 0 → 0.5: dark navy (20,20,40) → blue-600 (37,99,235)
    const t = clamped * 2
    r = Math.round(20 + t * (37 - 20))
    g = Math.round(20 + t * (99 - 20))
    b = Math.round(40 + t * (235 - 40))
  } else {
    // 0.5 → 1.0: blue-600 (37,99,235) → blue-200 (191,219,254)
    const t = (clamped - 0.5) * 2
    r = Math.round(37 + t * (191 - 37))
    g = Math.round(99 + t * (219 - 99))
    b = Math.round(235 + t * (254 - 235))
  }
  return `rgb(${r},${g},${b})`
}

function cellTitle(seasonYear: number, month: number, cell: HeatmapCell | undefined): string {
  const seasonLabel = formatSeasonLabel(seasonYear)
  const monthLabel = HEATMAP_MONTH_LABELS[month]
  if (!cell || isNaN(cell.winPct)) return `${seasonLabel} · ${monthLabel}\nNo data`
  const pct = (cell.winPct * 100).toFixed(1)
  return `${seasonLabel} · ${monthLabel}\n${cell.wins}W–${cell.losses}L · ${pct}%`
}

export default function MonthlyHeatmap({ data, seasonYears }: MonthlyHeatmapProps) {
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
                    const color = winPctToColor(cell?.winPct ?? NaN)
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
              background:
                "linear-gradient(to right, rgb(20,20,40), rgb(37,99,235), rgb(191,219,254))",
            }}
          />
          <span className="text-[10px] text-muted-foreground">100%</span>
        </div>
        <p className="mt-1 text-[10px] text-muted-foreground">
          Hover cells for details · Gray = no data
        </p>
      </CardContent>
    </Card>
  )
}
