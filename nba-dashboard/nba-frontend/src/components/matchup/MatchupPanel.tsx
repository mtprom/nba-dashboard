import { Card } from "@/components/ui/card"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Separator } from "@/components/ui/separator"
import MatchupScoreSummary from "./MatchupScoreSummary"
import MatchupHistoryTable from "./MatchupHistoryTable"
import PlayersToWatch from "@/components/players/PlayersToWatch"
import type { MatchupHistory, PlayerSeasonAvg } from "@/types"

interface MatchupPanelProps {
  matchup: MatchupHistory
  seasonAverages: Record<number, PlayerSeasonAvg>
  seasonAveragesError: boolean
}

export default function MatchupPanel({
  matchup,
  seasonAverages,
  seasonAveragesError,
}: MatchupPanelProps) {
  return (
    <Card className="mt-6">
      <div className="p-6">
        <MatchupScoreSummary matchup={matchup} />
        <Separator className="my-4" />

        <Tabs defaultValue="history">
          <TabsList className="mb-4">
            <TabsTrigger value="history">Matchup History</TabsTrigger>
            <TabsTrigger value="players">Players to Watch</TabsTrigger>
          </TabsList>

          <TabsContent value="history">
            <MatchupHistoryTable games={matchup.games} />
          </TabsContent>

          <TabsContent value="players">
            <PlayersToWatch
              matchup={matchup}
              seasonAverages={seasonAverages}
              seasonAveragesError={seasonAveragesError}
            />
          </TabsContent>
        </Tabs>
      </div>
    </Card>
  )
}
