import {
  Bar,
  CartesianGrid,
  ComposedChart,
  Legend,
  Line,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import type { PlayerHistorySeasonDatum } from "@/types/player-history"

interface PlayerRoleUsageChartProps {
  seasonStats: PlayerHistorySeasonDatum[]
  accentColor: string
}

export default function PlayerRoleUsageChart({
  seasonStats,
  accentColor,
}: PlayerRoleUsageChartProps) {
  const data = seasonStats.map((season) => ({
    seasonLabel: season.seasonLabel,
    usgPct: season.usgPct != null ? season.usgPct * 100 : null,
    astPct: season.astPct != null ? season.astPct * 100 : null,
    rebPct: season.rebPct != null ? season.rebPct * 100 : null,
    piePct: season.pie != null ? season.pie * 100 : null,
    netRating: season.netRating,
  }))

  return (
    <Card>
      <CardHeader className="pb-2">
        <CardTitle className="text-base font-medium">Role &amp; Usage</CardTitle>
      </CardHeader>
      <CardContent>
        <ResponsiveContainer width="100%" height={300}>
          <ComposedChart data={data} margin={{ top: 12, right: 16, left: -8, bottom: 48 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
            <XAxis
              dataKey="seasonLabel"
              tick={{ fontSize: 10, fill: "hsl(var(--muted-foreground))" }}
              angle={-45}
              textAnchor="end"
              height={60}
            />
            <YAxis
              yAxisId="pct"
              tick={{ fontSize: 11, fill: "hsl(var(--muted-foreground))" }}
              tickFormatter={(value) => `${value.toFixed(0)}%`}
            />
            <YAxis
              yAxisId="rating"
              orientation="right"
              tick={{ fontSize: 11, fill: "hsl(var(--muted-foreground))" }}
            />
            <Tooltip
              contentStyle={{
                backgroundColor: "hsl(var(--card))",
                border: "1px solid hsl(var(--border))",
                borderRadius: "6px",
                color: "hsl(var(--foreground))",
                fontSize: 12,
              }}
              formatter={(value: number | null, name: string) => {
                if (value == null) return ["—", name]
                if (name === "Net Rating") return [value.toFixed(1), name]
                return [`${value.toFixed(1)}%`, name]
              }}
            />
            <Legend wrapperStyle={{ fontSize: 11, paddingTop: 6 }} />
            <Bar yAxisId="pct" dataKey="usgPct" fill={accentColor} name="Usage %" radius={[3, 3, 0, 0]} />
            <Line yAxisId="pct" type="monotone" dataKey="astPct" stroke="#60a5fa" strokeWidth={2} name="Assist %" />
            <Line yAxisId="pct" type="monotone" dataKey="rebPct" stroke="#f59e0b" strokeWidth={2} name="Rebound %" />
            <Line yAxisId="pct" type="monotone" dataKey="piePct" stroke="#34d399" strokeWidth={2} name="PIE" />
            <Line yAxisId="rating" type="monotone" dataKey="netRating" stroke="#f87171" strokeWidth={2} name="Net Rating" />
          </ComposedChart>
        </ResponsiveContainer>
      </CardContent>
    </Card>
  )
}
