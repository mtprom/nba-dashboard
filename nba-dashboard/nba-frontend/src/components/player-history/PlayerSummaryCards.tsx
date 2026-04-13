import { Activity, Calendar, Timer, TrendingUp, BarChart3, Hand } from "lucide-react"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import type { PlayerHistoryMetrics } from "@/types/player-history"

interface PlayerSummaryCardsProps {
  metrics: PlayerHistoryMetrics
}

export default function PlayerSummaryCards({ metrics }: PlayerSummaryCardsProps) {
  const cards = [
    { label: "Total Games", value: metrics.totalGames.toString(), icon: Activity },
    { label: "Seasons Covered", value: metrics.seasonsCovered.toString(), icon: Calendar },
    { label: "Avg Minutes", value: `${metrics.avgMinutes.toFixed(1)} min`, icon: Timer },
    { label: "Avg Points", value: `${metrics.avgPoints.toFixed(1)} pts`, icon: TrendingUp },
    { label: "Avg Rebounds", value: `${metrics.avgRebounds.toFixed(1)} reb`, icon: BarChart3 },
    { label: "Avg Assists", value: `${metrics.avgAssists.toFixed(1)} ast`, icon: Hand },
  ]

  return (
    <div className="grid grid-cols-2 gap-4 xl:grid-cols-6">
      {cards.map(({ label, value, icon: Icon }) => (
        <Card key={label}>
          <CardHeader className="pb-2">
            <div className="flex items-center justify-between">
              <CardDescription>{label}</CardDescription>
              <Icon className="h-4 w-4 text-muted-foreground" />
            </div>
          </CardHeader>
          <CardContent>
            <CardTitle className="text-2xl">{value}</CardTitle>
          </CardContent>
        </Card>
      ))}
    </div>
  )
}
