import { useState, useEffect } from "react"
import Header from "@/components/layout/Header"
import PageContainer from "@/components/layout/PageContainer"
import HotPlayersTable from "@/components/hot/HotPlayersTable"
import HotTeamsTable from "@/components/hot/HotTeamsTable"
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/tabs"
import { Skeleton } from "@/components/ui/skeleton"
import { getHotPlayers, getHotTeams } from "@/api/hot"
import type { HotPlayersResponse, HotTeamsResponse } from "@/types"

type WindowOption = "5" | "10" | "season"

const WINDOW_LABELS: Record<WindowOption, string> = {
  "5": "Last 5 Games",
  "10": "Last 10 Games",
  season: "Full Season",
}

export default function HotPage() {
  const [window, setWindow] = useState<WindowOption>("5")
  const [players, setPlayers] = useState<HotPlayersResponse>({ hot: [], cold: [] })
  const [teams, setTeams] = useState<HotTeamsResponse>({ hot: [], cold: [] })
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false
    setLoading(true)

    async function load() {
      try {
        const [p, t] = await Promise.all([
          getHotPlayers(window),
          getHotTeams(window),
        ])
        if (!cancelled) {
          setPlayers(p)
          setTeams(t)
        }
      } catch (err) {
        console.error("Failed to load hot data", err)
      } finally {
        if (!cancelled) setLoading(false)
      }
    }
    load()

    return () => {
      cancelled = true
    }
  }, [window])

  const baselineLabel =
    window === "season" ? "last season" : "earlier games this season"

  return (
    <div>
      <Header />
      <PageContainer>
        <div className="mb-6">
          <h2 className="text-2xl font-bold">Hot & Cold</h2>
          <p className="text-sm text-muted-foreground">
            Players and teams trending up or down compared to {baselineLabel}
          </p>
        </div>

        {/* Window selector */}
        <div className="mb-6 flex gap-1 rounded-lg bg-muted p-1 w-fit">
          {(Object.entries(WINDOW_LABELS) as [WindowOption, string][]).map(
            ([value, label]) => (
              <button
                key={value}
                onClick={() => setWindow(value)}
                className={`rounded-md px-4 py-1.5 text-sm font-medium transition-colors ${
                  window === value
                    ? "bg-background text-foreground shadow-sm"
                    : "text-muted-foreground hover:text-foreground"
                }`}
              >
                {label}
              </button>
            )
          )}
        </div>

        {loading ? (
          <div className="space-y-3">
            {Array.from({ length: 10 }).map((_, i) => (
              <Skeleton key={i} className="h-14 w-full" />
            ))}
          </div>
        ) : (
          <div className="space-y-10">
            {/* Hot Players Section */}
            <section>
              <h3 className="text-lg font-semibold mb-3">Players</h3>
              <Tabs defaultValue="hot">
                <TabsList className="mb-3">
                  <TabsTrigger value="hot">
                    Hottest ({players.hot.length})
                  </TabsTrigger>
                  <TabsTrigger value="cold">
                    Coldest ({players.cold.length})
                  </TabsTrigger>
                </TabsList>
                <TabsContent value="hot">
                  <HotPlayersTable players={players.hot} />
                </TabsContent>
                <TabsContent value="cold">
                  <HotPlayersTable players={players.cold} />
                </TabsContent>
              </Tabs>
            </section>

            {/* Hot Teams Section */}
            <section>
              <h3 className="text-lg font-semibold mb-3">Teams</h3>
              <Tabs defaultValue="hot">
                <TabsList className="mb-3">
                  <TabsTrigger value="hot">
                    Hottest ({teams.hot.length})
                  </TabsTrigger>
                  <TabsTrigger value="cold">
                    Coldest ({teams.cold.length})
                  </TabsTrigger>
                </TabsList>
                <TabsContent value="hot">
                  <HotTeamsTable teams={teams.hot} />
                </TabsContent>
                <TabsContent value="cold">
                  <HotTeamsTable teams={teams.cold} />
                </TabsContent>
              </Tabs>
            </section>
          </div>
        )}
      </PageContainer>
    </div>
  )
}
