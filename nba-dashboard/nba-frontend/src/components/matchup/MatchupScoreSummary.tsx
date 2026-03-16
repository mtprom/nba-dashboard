import { getTeamColors } from "@/data/teams"
import type { MatchupHistory } from "@/types"

interface MatchupScoreSummaryProps {
  matchup: MatchupHistory
}

export default function MatchupScoreSummary({ matchup }: MatchupScoreSummaryProps) {
  const teamColors = getTeamColors(matchup.team.id)
  const oppColors = getTeamColors(matchup.opponent.id)
  const total = matchup.teamWins + matchup.opponentWins

  return (
    <div className="flex items-center justify-between gap-4">
      <div className="flex items-center gap-3">
        <div
          className="flex h-10 w-10 items-center justify-center rounded-full text-xs font-bold"
          style={{ backgroundColor: teamColors.primary, color: teamColors.secondary }}
        >
          {matchup.team.abbreviation}
        </div>
        <div>
          <div className="text-sm font-semibold">{matchup.team.fullName}</div>
          <div className="text-xs text-muted-foreground">
            <span style={{ color: teamColors.primary }} className="font-bold">
              {matchup.teamWins}
            </span>
            {" "}wins
          </div>
        </div>
      </div>

      <div className="text-center">
        <div className="text-xs text-muted-foreground">Last {total} Games</div>
        <div className="flex items-center gap-1 text-lg font-bold">
          <span style={{ color: teamColors.primary }}>{matchup.teamWins}</span>
          <span className="text-muted-foreground">-</span>
          <span style={{ color: oppColors.primary }}>{matchup.opponentWins}</span>
        </div>
      </div>

      <div className="flex items-center gap-3">
        <div className="text-right">
          <div className="text-sm font-semibold">{matchup.opponent.fullName}</div>
          <div className="text-xs text-muted-foreground">
            <span style={{ color: oppColors.primary }} className="font-bold">
              {matchup.opponentWins}
            </span>
            {" "}wins
          </div>
        </div>
        <div
          className="flex h-10 w-10 items-center justify-center rounded-full text-xs font-bold"
          style={{ backgroundColor: oppColors.primary, color: oppColors.secondary }}
        >
          {matchup.opponent.abbreviation}
        </div>
      </div>
    </div>
  )
}
