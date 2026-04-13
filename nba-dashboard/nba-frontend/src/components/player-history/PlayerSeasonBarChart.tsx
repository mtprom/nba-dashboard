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
  PlayerHistorySeasonDatum,
} from "@/types/player-history"

interface PlayerSeasonBarChartProps {
  seasonStats: PlayerHistorySeasonDatum[]
  metric: PlayerHistoryMetricKey
  accentColor: string
}

export default function PlayerSeasonBarChart({
  seasonStats,
  metric,
  accentColor,
}: PlayerSeasonBarChartProps) {
  const data = seasonStats.map((season) => ({
    seasonLabel: season.seasonLabel,
    gamesPlayed: season.gamesPlayed,
    value: getMetricValue(season, metric),
  }))

  return (
    <Card>
      <CardHeader className="pb-2">
        <CardTitle className="text-base font-medium">
          Season Summary Bars
        </CardTitle>
      </CardHeader>
      <CardContent>
        <ResponsiveContainer width="100%" height={280}>
          <BarChart data={data} margin={{ top: 12, right: 16, left: -10, bottom: 48 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
            <XAxis
              dataKey="seasonLabel"
              tick={{ fontSize: 10, fill: "hsl(var(--muted-foreground))" }}
              angle={-45}
              textAnchor="end"
              height={60}
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
              labelFormatter={(label) => `Season ${label}`}
            />
            <Bar dataKey="value" fill={accentColor} radius={[3, 3, 0, 0]} />
          </BarChart>
        </ResponsiveContainer>
      </CardContent>
    </Card>
  )
}
