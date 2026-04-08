import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Legend,
} from "recharts"
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card"
import { SeasonStatDatum } from "@/types/history"

interface GameTypeDistributionProps {
  data: SeasonStatDatum[]
}

const CLOSE_COLOR = "#60a5fa"     // blue-400
const MODERATE_COLOR = "#94a3b8"  // slate-400 (neutral)
const BLOWOUT_COLOR = "#f97316"   // orange-500

const LEGEND_LABELS: Record<string, string> = {
  closeGames: "Close (≤5 pts)",
  moderateGames: "Moderate (6–19 pts)",
  blowoutGames: "Blowout (≥20 pts)",
}

export default function GameTypeDistribution({ data }: GameTypeDistributionProps) {
  const filtered = data.filter((d) => d.gameCount > 0)

  return (
    <Card>
      <CardHeader className="pb-2">
        <CardTitle className="text-base font-medium">Game Margin Distribution</CardTitle>
      </CardHeader>
      <CardContent>
        <ResponsiveContainer width="100%" height={300}>
          <BarChart data={filtered} margin={{ top: 12, right: 16, left: -8, bottom: 48 }}>
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
              allowDecimals={false}
            />
            <Tooltip
              contentStyle={{
                backgroundColor: "hsl(var(--card))",
                border: "1px solid hsl(var(--border))",
                borderRadius: "6px",
                color: "hsl(var(--foreground))",
                fontSize: 12,
              }}
              formatter={(value: number, name: string) => [value, LEGEND_LABELS[name] ?? name]}
              labelFormatter={(label) => `Season ${label}`}
            />
            <Legend
              formatter={(value) => LEGEND_LABELS[value] ?? value}
              wrapperStyle={{ fontSize: 11, paddingTop: 4 }}
            />
            <Bar dataKey="closeGames" stackId="a" fill={CLOSE_COLOR} radius={[0, 0, 0, 0]} />
            <Bar dataKey="moderateGames" stackId="a" fill={MODERATE_COLOR} radius={[0, 0, 0, 0]} />
            <Bar dataKey="blowoutGames" stackId="a" fill={BLOWOUT_COLOR} radius={[3, 3, 0, 0]} />
          </BarChart>
        </ResponsiveContainer>
      </CardContent>
    </Card>
  )
}
