import {
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import {
  formatMetricValue,
  formatPlayerHistoryDate,
  getMetricValue,
  metricAxisLabel,
  metricLabel,
  PLAYER_HISTORY_METRICS,
} from "@/data/player-history"
import type {
  PlayerHistoryGame,
  PlayerHistoryGranularity,
  PlayerHistoryMetricKey,
  PlayerHistorySeasonDatum,
} from "@/types/player-history"

interface PlayerTrendChartProps {
  games: PlayerHistoryGame[]
  seasonStats: PlayerHistorySeasonDatum[]
  metric: PlayerHistoryMetricKey
  granularity: PlayerHistoryGranularity
  onMetricChange: (metric: PlayerHistoryMetricKey) => void
  accentColor: string
}

const selectClass =
  "bg-muted text-foreground text-sm rounded-md border border-border px-3 py-1.5 focus:outline-none focus:ring-2 focus:ring-ring"

export default function PlayerTrendChart({
  games,
  seasonStats,
  metric,
  granularity,
  onMetricChange,
  accentColor,
}: PlayerTrendChartProps) {
  const data = granularity === "game"
    ? [...games]
        .sort((a, b) => a.date.localeCompare(b.date))
        .map((game, index) => ({
          key: game.gameId,
          label: formatPlayerHistoryDate(game.date),
          fullLabel: `${formatPlayerHistoryDate(game.date)} · ${game.won ? "W" : "L"}`,
          value: getMetricValue(game, metric),
          index: index + 1,
        }))
    : seasonStats.map((season) => ({
        key: season.seasonYear,
        label: season.seasonLabel,
        fullLabel: season.seasonLabel,
        value: getMetricValue(season, metric),
        index: season.seasonYear,
      }))

  const xKey = granularity === "game" ? "index" : "label"
  const showDots = data.length <= 24

  return (
    <Card>
      <CardHeader className="flex flex-col gap-3 pb-2 md:flex-row md:items-center md:justify-between">
        <div>
          <CardTitle className="text-base font-medium">
            {metricLabel(metric)} Trend
          </CardTitle>
          <p className="mt-1 text-sm text-muted-foreground">
            {granularity === "game" ? "Game-by-game box score trend" : "Season average trend"}
          </p>
        </div>
        <div className="flex flex-col gap-1">
          <label className="text-xs font-medium text-muted-foreground">Metric</label>
          <select
            className={selectClass}
            value={metric}
            onChange={(e) => onMetricChange(e.target.value as PlayerHistoryMetricKey)}
          >
            {Object.entries(PLAYER_HISTORY_METRICS).map(([key, value]) => (
              <option key={key} value={key}>
                {value.label}
              </option>
            ))}
          </select>
        </div>
      </CardHeader>
      <CardContent>
        <ResponsiveContainer width="100%" height={320}>
          <LineChart data={data} margin={{ top: 12, right: 16, left: -10, bottom: 40 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
            <XAxis
              dataKey={xKey}
              tick={{ fontSize: 10, fill: "hsl(var(--muted-foreground))" }}
              tickFormatter={(value) => {
                if (granularity === "game") {
                  if (typeof value !== "number") return ""
                  return value
                }
                return value
              }}
              interval={Math.max(0, Math.floor(data.length / 10) - 1)}
              angle={granularity === "season" ? -45 : 0}
              textAnchor={granularity === "season" ? "end" : "middle"}
              height={granularity === "season" ? 60 : 30}
            />
            <YAxis
              tick={{ fontSize: 11, fill: "hsl(var(--muted-foreground))" }}
              label={{
                value: metricAxisLabel(metric),
                angle: -90,
                position: "insideLeft",
                offset: 12,
                style: { fontSize: 10, fill: "hsl(var(--muted-foreground))" },
              }}
            />
            <Tooltip
              contentStyle={{
                backgroundColor: "hsl(var(--card))",
                border: "1px solid hsl(var(--border))",
                borderRadius: "6px",
                color: "hsl(var(--foreground))",
                fontSize: 12,
              }}
              formatter={(value: number | null) => [formatMetricValue(metric, value), metricLabel(metric)]}
              labelFormatter={(_label, payload) => payload?.[0]?.payload?.fullLabel ?? ""}
            />
            <Line
              type="monotone"
              dataKey="value"
              stroke={accentColor}
              strokeWidth={2}
              dot={showDots ? { r: 3, fill: accentColor, strokeWidth: 0 } : false}
              activeDot={{ r: 5 }}
              connectNulls={false}
            />
          </LineChart>
        </ResponsiveContainer>
      </CardContent>
    </Card>
  )
}
