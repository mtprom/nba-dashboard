import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Cell,
} from "recharts"
import {
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  CardContent,
} from "@/components/ui/card"
import { LeaguePlacement, LeaguePlacementTeam } from "@/types/history"
import { getTeamColorsDark } from "@/data/teams"

interface LeaguePlacementChartProps {
  data: LeaguePlacement
  teamColor: string
}

function pct(v: number) {
  return `${(v * 100).toFixed(1)}%`
}

function withOpacity(hex: string, opacity: number): string {
  const r = parseInt(hex.slice(1, 3), 16)
  const g = parseInt(hex.slice(3, 5), 16)
  const b = parseInt(hex.slice(5, 7), 16)
  return `rgba(${r},${g},${b},${opacity})`
}

function PlacementTooltip({
  active,
  payload,
}: {
  active?: boolean
  payload?: Array<{ payload: LeaguePlacementTeam }>
}) {
  if (!active || !payload?.length) return null

  const team = payload[0].payload

  return (
    <div
      className="rounded-md border px-3 py-2 text-xs shadow-sm"
      style={{
        backgroundColor: "hsl(var(--card))",
        borderColor: "hsl(var(--border))",
        color: "hsl(var(--foreground))",
      }}
    >
      <div className="font-medium">{team.teamName}</div>
      <div className="text-[11px] text-muted-foreground">
        {team.wins}-{team.losses} record · {pct(team.winPct)}
      </div>
      <div className="mt-1 text-[11px]">
        #{team.leagueRank} NBA · #{team.conferenceRank} {team.conference}
      </div>
    </div>
  )
}

export default function LeaguePlacementChart({ data, teamColor }: LeaguePlacementChartProps) {
  const chartHeight = Math.max(360, data.teams.length * 24 + 24)

  return (
    <Card>
      <CardHeader className="pb-2">
        <CardTitle className="text-base font-medium">League Placement</CardTitle>
        <CardDescription>
          #{data.selectedLeagueRank} NBA · #{data.selectedConferenceRank} {data.selectedConference}
        </CardDescription>
      </CardHeader>
      <CardContent>
        <ResponsiveContainer width="100%" height={chartHeight}>
          <BarChart
            data={data.teams}
            layout="vertical"
            margin={{ top: 8, right: 24, left: 8, bottom: 8 }}
            barCategoryGap={5}
          >
            <CartesianGrid
              strokeDasharray="3 3"
              stroke="hsl(var(--border))"
              horizontal={false}
            />
            <XAxis
              type="number"
              domain={[0, 1]}
              tickFormatter={pct}
              tick={{ fontSize: 11, fill: "hsl(var(--muted-foreground))" }}
            />
            <YAxis
              type="category"
              dataKey="abbreviation"
              width={44}
              tick={{ fontSize: 11, fill: "hsl(var(--muted-foreground))" }}
            />
            <Tooltip
              cursor={{ fill: "rgba(148, 163, 184, 0.08)" }}
              content={<PlacementTooltip />}
            />
            <Bar dataKey="winPct" radius={[0, 4, 4, 0]}>
              {data.teams.map((team) => {
                const isSelected = team.teamId === data.selectedTeamId
                const colors = getTeamColorsDark(team.teamId)
                const fill = isSelected
                  ? colors.primary
                  : withOpacity(colors.primary, 0.82)

                return (
                  <Cell
                    key={team.teamId}
                    fill={fill}
                    stroke={isSelected ? colors.secondary : "none"}
                    strokeWidth={isSelected ? 1.5 : 0}
                  />
                )
              })}
            </Bar>
          </BarChart>
        </ResponsiveContainer>
      </CardContent>
    </Card>
  )
}
