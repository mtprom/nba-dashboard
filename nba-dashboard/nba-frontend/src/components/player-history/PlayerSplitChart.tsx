import {
  Bar,
  BarChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import {
  formatMetricValue,
  getMetricValue,
  metricLabel,
} from "@/data/player-history"
import type {
  PlayerHistoryMetricKey,
  PlayerHistorySplit,
} from "@/types/player-history"

interface PlayerSplitChartProps {
  title: string
  data: PlayerHistorySplit[]
  metric: PlayerHistoryMetricKey
  accentColor: string
}

export default function PlayerSplitChart({
  title,
  data,
  metric,
  accentColor,
}: PlayerSplitChartProps) {
  const chartData = data.map((row) => ({
    label: row.label,
    gamesPlayed: row.gamesPlayed,
    value: getMetricValue(row, metric),
  }))

  return (
    <Card>
      <CardHeader className="pb-2">
        <CardTitle className="text-base font-medium">{title}</CardTitle>
      </CardHeader>
      <CardContent>
        <ResponsiveContainer width="100%" height={250}>
          <BarChart data={chartData} margin={{ top: 12, right: 16, left: -10, bottom: 12 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
            <XAxis
              dataKey="label"
              tick={{ fontSize: 11, fill: "hsl(var(--muted-foreground))" }}
            />
            <YAxis tick={{ fontSize: 11, fill: "hsl(var(--muted-foreground))" }} />
            <Tooltip
              contentStyle={{
                backgroundColor: "hsl(var(--card))",
                border: "1px solid hsl(var(--border))",
                borderRadius: "6px",
                color: "hsl(var(--foreground))",
                fontSize: 12,
              }}
              formatter={(value: number | null) => [formatMetricValue(metric, value), metricLabel(metric)]}
              labelFormatter={(label, payload) => {
                const gamesPlayed = payload?.[0]?.payload?.gamesPlayed
                return `${label} · ${gamesPlayed ?? 0} games`
              }}
            />
            <Bar dataKey="value" fill={accentColor} radius={[3, 3, 0, 0]} maxBarSize={56} />
          </BarChart>
        </ResponsiveContainer>
      </CardContent>
    </Card>
  )
}
