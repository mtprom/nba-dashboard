import { Activity, Calendar, TrendingUp } from "lucide-react"
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card"
import { HistoryMetrics } from "@/types/history"

interface SummaryCardsProps {
  metrics: HistoryMetrics
}

export default function SummaryCards({ metrics }: SummaryCardsProps) {
  const cards = [
    {
      icon: Activity,
      label: "Total Games",
      value: metrics.totalGames.toString(),
    },
    {
      icon: Calendar,
      label: "Seasons Covered",
      value: metrics.seasonsCovered.toString(),
    },
    {
      icon: TrendingUp,
      label: "Avg Margin of Victory",
      value: metrics.totalGames > 0 ? `${metrics.avgMarginOfVictory.toFixed(1)} pts` : "—",
    },
  ]

  return (
    <div className="grid grid-cols-2 lg:grid-cols-3 gap-4">
      {cards.map(({ icon: Icon, label, value }) => (
        <Card key={label}>
          <CardHeader className="pb-2">
            <div className="flex items-center justify-between">
              <CardDescription>{label}</CardDescription>
              <Icon className="h-4 w-4 text-muted-foreground" />
            </div>
          </CardHeader>
          <CardContent>
            <CardTitle className="text-3xl">{value}</CardTitle>
          </CardContent>
        </Card>
      ))}
    </div>
  )
}
