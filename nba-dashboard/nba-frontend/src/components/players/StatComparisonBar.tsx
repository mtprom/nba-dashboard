import { Badge } from "@/components/ui/badge"

interface StatComparisonBarProps {
  label: string
  seasonAvg: number
  vsOpponentAvg: number
  teamColor: string
  format?: "number" | "pct"
}

export default function StatComparisonBar({
  label,
  seasonAvg,
  vsOpponentAvg,
  teamColor,
  format = "number",
}: StatComparisonBarProps) {
  const delta = vsOpponentAvg - seasonAvg
  const isPositive = delta > 0

  const fmt = (v: number) =>
    format === "pct" ? `${(v * 100).toFixed(1)}%` : v.toFixed(1)

  const deltaFmt = format === "pct"
    ? `${isPositive ? "+" : ""}${(delta * 100).toFixed(1)}%`
    : `${isPositive ? "+" : ""}${delta.toFixed(1)}`

  const max = Math.max(seasonAvg, vsOpponentAvg) * 1.2 || 1
  const seasonWidth = (seasonAvg / max) * 100
  const vsWidth = (vsOpponentAvg / max) * 100

  return (
    <div className="space-y-1">
      <div className="flex items-center justify-between">
        <span className="text-xs font-medium text-muted-foreground">{label}</span>
        <Badge
          variant={isPositive ? "default" : "secondary"}
          className="h-5 px-1.5 text-[10px] font-mono"
          style={isPositive ? { backgroundColor: teamColor, color: "#fff" } : undefined}
        >
          {deltaFmt}
        </Badge>
      </div>

      <div className="space-y-1">
        {/* Season avg bar */}
        <div className="flex items-center gap-2">
          <span className="w-12 text-right text-[10px] text-muted-foreground">Season</span>
          <div className="relative h-3 flex-1 overflow-hidden rounded-full bg-muted">
            <div
              className="h-full rounded-full bg-muted-foreground/30"
              style={{ width: `${seasonWidth}%` }}
            />
          </div>
          <span className="w-12 text-right font-mono text-xs text-muted-foreground">
            {fmt(seasonAvg)}
          </span>
        </div>

        {/* Vs opponent bar */}
        <div className="flex items-center gap-2">
          <span className="w-12 text-right text-[10px] text-foreground">vs OPP</span>
          <div className="relative h-3 flex-1 overflow-hidden rounded-full bg-muted">
            <div
              className="h-full rounded-full"
              style={{ width: `${vsWidth}%`, backgroundColor: teamColor }}
            />
          </div>
          <span className="w-12 text-right font-mono text-xs font-semibold">
            {fmt(vsOpponentAvg)}
          </span>
        </div>
      </div>
    </div>
  )
}
