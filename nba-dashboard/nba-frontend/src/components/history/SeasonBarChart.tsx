import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  LabelList,
} from "recharts"
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card"
import { SeasonBarDatum } from "@/types/history"

interface SeasonBarChartProps {
  data: SeasonBarDatum[]
}

// Custom label rendered above bars with annotations (lockout / bubble)
function AnnotationLabel(props: {
  x?: number
  y?: number
  width?: number
  value?: string
}) {
  const { x = 0, y = 0, width = 0, value } = props
  if (!value) return null
  return (
    <text
      x={x + width / 2}
      y={y - 4}
      textAnchor="middle"
      fontSize={8}
      fill="hsl(var(--muted-foreground))"
    >
      {value}
    </text>
  )
}

export default function SeasonBarChart({ data }: SeasonBarChartProps) {
  return (
    <Card>
      <CardHeader className="pb-2">
        <CardTitle className="text-base font-medium">Games Per Season</CardTitle>
      </CardHeader>
      <CardContent>
        <ResponsiveContainer width="100%" height={280}>
          <BarChart data={data} margin={{ top: 20, right: 8, left: -16, bottom: 48 }}>
            <CartesianGrid
              strokeDasharray="3 3"
              stroke="hsl(var(--border))"
              vertical={false}
            />
            <XAxis
              dataKey="seasonLabel"
              tick={{ fontSize: 10, fill: "hsl(var(--muted-foreground))" }}
              interval={2}
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
              formatter={(value: number) => [value, "Games"]}
              labelFormatter={(label) => `Season ${label}`}
            />
            <Bar dataKey="gameCount" fill="hsl(var(--primary))" radius={[3, 3, 0, 0]}>
              <LabelList dataKey="annotation" content={<AnnotationLabel />} />
            </Bar>
          </BarChart>
        </ResponsiveContainer>
      </CardContent>
    </Card>
  )
}
