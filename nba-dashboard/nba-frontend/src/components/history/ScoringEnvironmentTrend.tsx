import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from "recharts"
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card"
import { SeasonStatDatum } from "@/types/history"

interface ScoringEnvironmentTrendProps {
  data: SeasonStatDatum[]
}

export default function ScoringEnvironmentTrend({ data }: ScoringEnvironmentTrendProps) {
  const filtered = data.filter((d) => d.avgTotalPoints !== null && d.gameCount > 0)

  return (
    <Card>
      <CardHeader className="pb-2">
        <CardTitle className="text-base font-medium">Scoring Environment</CardTitle>
      </CardHeader>
      <CardContent>
        <ResponsiveContainer width="100%" height={260}>
          <LineChart data={filtered} margin={{ top: 12, right: 16, left: -8, bottom: 48 }}>
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
              tick={{ fontSize: 11, fill: "hsl(var(--muted-foreground))" }}
              domain={["auto", "auto"]}
              label={{
                value: "Avg Pts/Game",
                angle: -90,
                position: "insideLeft",
                offset: 16,
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
              formatter={(value: number) => [`${value.toFixed(1)} pts`, "Avg Total Points"]}
              labelFormatter={(label) => `Season ${label}`}
            />
            <Line
              type="monotone"
              dataKey="avgTotalPoints"
              stroke="hsl(var(--primary))"
              strokeWidth={2}
              dot={{ r: 3, fill: "hsl(var(--primary))", strokeWidth: 0 }}
              activeDot={{ r: 5 }}
              connectNulls={false}
            />
          </LineChart>
        </ResponsiveContainer>
      </CardContent>
    </Card>
  )
}
