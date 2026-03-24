import { useState, useEffect, useMemo } from "react"
import Header from "@/components/layout/Header"
import PageContainer from "@/components/layout/PageContainer"
import StandingsTable from "@/components/standings/StandingsTable"
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/tabs"
import { Skeleton } from "@/components/ui/skeleton"
import { getStandings } from "@/api/standings"
import type { StandingsEntry } from "@/types"

export default function StandingsPage() {
  const [standings, setStandings] = useState<StandingsEntry[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    async function load() {
      try {
        const data = await getStandings()
        setStandings(data)
      } catch (err) {
        console.error("Failed to load standings", err)
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [])

  const eastern = useMemo(
    () =>
      standings
        .filter((s) => s.conference.toLowerCase().startsWith("east"))
        .sort((a, b) => a.confRank - b.confRank),
    [standings]
  )

  const western = useMemo(
    () =>
      standings
        .filter((s) => s.conference.toLowerCase().startsWith("west"))
        .sort((a, b) => a.confRank - b.confRank),
    [standings]
  )

  return (
    <div>
      <Header />
      <PageContainer>
        <div className="mb-6">
          <h2 className="text-2xl font-bold">Standings</h2>
          <p className="text-sm text-muted-foreground">
            2025-26 Regular Season
          </p>
        </div>

        {loading ? (
          <div className="space-y-3">
            {Array.from({ length: 15 }).map((_, i) => (
              <Skeleton key={i} className="h-12 w-full" />
            ))}
          </div>
        ) : (
          <Tabs defaultValue="east">
            <TabsList className="mb-4">
              <TabsTrigger value="east">Eastern Conference</TabsTrigger>
              <TabsTrigger value="west">Western Conference</TabsTrigger>
            </TabsList>

            <TabsContent value="east">
              <StandingsTable standings={eastern} />
            </TabsContent>

            <TabsContent value="west">
              <StandingsTable standings={western} />
            </TabsContent>
          </Tabs>
        )}
      </PageContainer>
    </div>
  )
}
