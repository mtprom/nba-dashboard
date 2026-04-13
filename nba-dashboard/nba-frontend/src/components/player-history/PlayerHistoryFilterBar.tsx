import { useState } from "react"
import type {
  PlayerHistoryFilter,
  PlayerHistorySearchResult,
} from "@/types/player-history"

interface PlayerHistoryFilterBarProps {
  filter: PlayerHistoryFilter
  searchQuery: string
  onSearchQueryChange: (value: string) => void
  onSelectPlayer: (player: PlayerHistorySearchResult) => void
  searchResults: PlayerHistorySearchResult[]
  availableSeasonYears: number[]
  onFilterChange: (filter: PlayerHistoryFilter) => void
  onReset: () => void
  isSearching: boolean
}

const selectClass =
  "bg-muted text-foreground text-sm rounded-md border border-border px-3 py-1.5 focus:outline-none focus:ring-2 focus:ring-ring"

export default function PlayerHistoryFilterBar({
  filter,
  searchQuery,
  onSearchQueryChange,
  onSelectPlayer,
  searchResults,
  availableSeasonYears,
  onFilterChange,
  onReset,
  isSearching,
}: PlayerHistoryFilterBarProps) {
  const [open, setOpen] = useState(false)

  function handleFrom(value: string) {
    const fromSeason = Number(value)
    onFilterChange({
      ...filter,
      fromSeason,
      toSeason: Math.max(fromSeason, filter.toSeason),
    })
  }

  function handleTo(value: string) {
    const toSeason = Number(value)
    onFilterChange({
      ...filter,
      toSeason,
      fromSeason: Math.min(filter.fromSeason, toSeason),
    })
  }

  const canShowResults = open && searchQuery.trim().length >= 2

  return (
    <div className="flex flex-wrap items-end gap-4">
      <div className="relative flex min-w-[280px] flex-col gap-1">
        <label className="text-xs font-medium text-muted-foreground">Player</label>
        <input
          className="bg-muted text-foreground text-sm rounded-md border border-border px-3 py-1.5 focus:outline-none focus:ring-2 focus:ring-ring"
          value={searchQuery}
          onChange={(e) => {
            setOpen(true)
            onSearchQueryChange(e.target.value)
          }}
          onFocus={() => setOpen(true)}
          onBlur={() => window.setTimeout(() => setOpen(false), 120)}
          placeholder="Search players..."
        />
        {canShowResults && (
          <div className="absolute left-0 top-full z-20 mt-1 max-h-72 w-full overflow-y-auto rounded-md border border-border bg-card shadow-lg">
            {isSearching && (
              <div className="px-3 py-2 text-sm text-muted-foreground">Searching…</div>
            )}
            {!isSearching && searchResults.length === 0 && (
              <div className="px-3 py-2 text-sm text-muted-foreground">
                No matching players.
              </div>
            )}
            {!isSearching && searchResults.map((player) => (
              <button
                key={player.playerId}
                type="button"
                className="flex w-full flex-col gap-0.5 border-b border-border/60 px-3 py-2 text-left last:border-b-0 hover:bg-muted/60"
                onMouseDown={(e) => e.preventDefault()}
                onClick={() => {
                  onSelectPlayer(player)
                  setOpen(false)
                }}
              >
                <span className="text-sm font-medium text-foreground">
                  {player.playerName}
                </span>
                <span className="text-xs text-muted-foreground">
                  {player.position || "—"} · {player.currentTeamAbbreviation || "FA"} · {player.firstSeasonYear}-{String(player.lastSeasonYear + 1).slice(2)}
                </span>
              </button>
            ))}
          </div>
        )}
      </div>

      <div className="flex flex-col gap-1">
        <label className="text-xs font-medium text-muted-foreground">From Season</label>
        <select
          className={selectClass}
          value={filter.fromSeason}
          disabled={availableSeasonYears.length === 0}
          onChange={(e) => handleFrom(e.target.value)}
        >
          {availableSeasonYears.map((year) => (
            <option key={year} value={year} disabled={year > filter.toSeason}>
              {year}-{String(year + 1).slice(2)}
            </option>
          ))}
        </select>
      </div>

      <div className="flex flex-col gap-1">
        <label className="text-xs font-medium text-muted-foreground">To Season</label>
        <select
          className={selectClass}
          value={filter.toSeason}
          disabled={availableSeasonYears.length === 0}
          onChange={(e) => handleTo(e.target.value)}
        >
          {availableSeasonYears.map((year) => (
            <option key={year} value={year} disabled={year < filter.fromSeason}>
              {year}-{String(year + 1).slice(2)}
            </option>
          ))}
        </select>
      </div>

      <div className="flex flex-col gap-1">
        <label className="text-xs font-medium text-muted-foreground">Trend View</label>
        <div className="flex rounded-md border border-border bg-muted p-0.5">
          {(["game", "season"] as const).map((value) => (
            <button
              key={value}
              type="button"
              className={`rounded px-3 py-1 text-sm font-medium transition-colors ${
                filter.granularity === value
                  ? "bg-background text-foreground shadow-sm"
                  : "text-muted-foreground hover:text-foreground"
              }`}
              onClick={() => onFilterChange({ ...filter, granularity: value })}
              disabled={filter.playerId === null}
            >
              {value === "game" ? "Game" : "Season"}
            </button>
          ))}
        </div>
      </div>

      <button
        type="button"
        onClick={onReset}
        className="rounded-md border border-border bg-muted px-3 py-1.5 text-sm font-medium text-foreground transition-colors hover:bg-muted/80"
      >
        Reset
      </button>
    </div>
  )
}
