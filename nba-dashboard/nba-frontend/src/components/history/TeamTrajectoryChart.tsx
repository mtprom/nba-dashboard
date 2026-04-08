import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  ReferenceLine,
} from "recharts"
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card"
import { SeasonStatDatum } from "@/types/history"

interface TeamTrajectoryChartProps {
  data: SeasonStatDatum[]
  teamColor: string
}

function pct(v: number) {
  return `${(v * 100).toFixed(0)}%`
}

export default function TeamTrajectoryChart({ data, teamColor }: TeamTrajectoryChartProps) {
  const filtered = data.filter((d) => d.winPct !== null && d.gameCount > 0)

  return (
    <Card>
      <CardHeader className="pb-2">
        <CardTitle className="text-base font-medium">Season Trajectory</CardTitle>
      </CardHeader>
      <CardContent>
        <ResponsiveContainer width="100%" height={280}>
          <LineChart data={filtered} margin={{ top: 12, right: 16, left: -16, bottom: 48 }}>
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
              formatter={(value: number, _name: string, props) => {
                const d = props.payload as SeasonStatDatum
                const wins = Math.round(d.gameCount * value)
                const losses = d.gameCount - wins
                return [`${pct(value)} (${wins}–${losses})`, "Win %"]
              }}
              labelFormatter={(label) => `Season ${label}`}
            />
            <Line
              type="monotone"
              dataKey="winPct"
              stroke={teamColor}
              strokeWidth={2}
              dot={{ r: 3, fill: teamColor, strokeWidth: 0 }}
              activeDot={{ r: 5 }}
              connectNulls={false}
            />
          </LineChart>
        </ResponsiveContainer>
      </CardContent>
    </Card>
  )
}
