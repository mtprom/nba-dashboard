import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  ReferenceLine,
  Legend,
} from "recharts"
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card"
import { SeasonStatDatum } from "@/types/history"

interface HomeAwayWinRateSplitProps {
  data: SeasonStatDatum[]
  teamColor: string
}

function pct(v: number) {
  return `${(v * 100).toFixed(0)}%`
}

// Hex color → rgba with given opacity
function withOpacity(hex: string, opacity: number): string {
  const r = parseInt(hex.slice(1, 3), 16)
  const g = parseInt(hex.slice(3, 5), 16)
  const b = parseInt(hex.slice(5, 7), 16)
  return `rgba(${r},${g},${b},${opacity})`
}

export default function HomeAwayWinRateSplit({ data, teamColor }: HomeAwayWinRateSplitProps) {
  const filtered = data.filter((d) => d.homeWinPct !== null || d.awayWinPct !== null)

  const homeColor = teamColor
  const awayColor = teamColor.startsWith("#") ? withOpacity(teamColor, 0.5) : teamColor

  return (
    <Card>
      <CardHeader className="pb-2">
        <CardTitle className="text-base font-medium">Home vs Away Win Rate</CardTitle>
      </CardHeader>
      <CardContent>
        <ResponsiveContainer width="100%" height={280}>
          <BarChart data={filtered} margin={{ top: 12, right: 16, left: -16, bottom: 48 }} barGap={2}>
            <CartesianGrid
              strokeDasharray="3 3"
              stroke="hsl(var(--border))"
              vertical={false}
            />
            <XAxis
              dataKey="seasonLabel"
              tick={{ fontSize: 10, fill: "hsl(var(--muted-foreground))" }}
              interval={Math.max(0, Math.floor(filtered.length / 10) - 1)}
              angle={-45}
              textAnchor="end"
              height={60}
            />
            <YAxis
              domain={[0, 1]}
              tickFormatter={pct}
              tick={{ fontSize: 11, fill: "hsl(var(--muted-foreground))" }}
            />
            <ReferenceLine
              y={0.5}
              stroke="hsl(var(--muted-foreground))"
              strokeDasharray="4 4"
              strokeOpacity={0.5}
            />
            <Tooltip
              contentStyle={{
                backgroundColor: "hsl(var(--card))",
                border: "1px solid hsl(var(--border))",
                borderRadius: "6px",
                color: "hsl(var(--foreground))",
                fontSize: 12,
              }}
              formatter={(value: number, name: string) => [
                value != null ? pct(value) : "—",
                name === "homeWinPct" ? "Home" : "Away",
              ]}
              labelFormatter={(label) => `Season ${label}`}
            />
            <Legend
              formatter={(value) => (value === "homeWinPct" ? "Home" : "Away")}
              wrapperStyle={{ fontSize: 11, paddingTop: 4 }}
            />
            <Bar dataKey="homeWinPct" fill={homeColor} radius={[2, 2, 0, 0]} maxBarSize={16} />
            <Bar dataKey="awayWinPct" fill={awayColor} radius={[2, 2, 0, 0]} maxBarSize={16} />
          </BarChart>
        </ResponsiveContainer>
      </CardContent>
    </Card>
  )
}
