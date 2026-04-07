import { HistoryFilter } from "@/types/history"
import { TEAM_INFO } from "@/data/teams"

interface FilterBarProps {
  filter: HistoryFilter
  onFilterChange: (filter: HistoryFilter) => void
  onReset: () => void
}

const SEASONS = Array.from({ length: 31 }, (_, i) => 1994 + i) // 1994–2024

const SORTED_TEAMS = Object.entries(TEAM_INFO).sort((a, b) =>
  a[1].fullName.localeCompare(b[1].fullName)
)

const selectClass =
  "bg-muted text-foreground text-sm rounded-md border border-border px-3 py-1.5 focus:outline-none focus:ring-2 focus:ring-ring"

export default function FilterBar({ filter, onFilterChange, onReset }: FilterBarProps) {
  function handleTeam(e: React.ChangeEvent<HTMLSelectElement>) {
    const val = e.target.value
    onFilterChange({ ...filter, teamId: val === "" ? null : Number(val) })
  }

  function handleFrom(e: React.ChangeEvent<HTMLSelectElement>) {
    const from = Number(e.target.value)
    onFilterChange({
      ...filter,
      fromSeason: from,
      toSeason: Math.max(from, filter.toSeason),
    })
  }

  function handleTo(e: React.ChangeEvent<HTMLSelectElement>) {
    const to = Number(e.target.value)
    onFilterChange({
      ...filter,
      toSeason: to,
      fromSeason: Math.min(filter.fromSeason, to),
    })
  }

  return (
    <div className="flex flex-wrap items-end gap-4">
      {/* Team selector */}
      <div className="flex flex-col gap-1">
        <label className="text-xs text-muted-foreground font-medium">Team</label>
        <select
          className={selectClass}
          value={filter.teamId ?? ""}
          onChange={handleTeam}
        >
          <option value="">All Teams</option>
          {SORTED_TEAMS.map(([id, info]) => (
            <option key={id} value={id}>
              {info.fullName}
            </option>
          ))}
        </select>
      </div>

      {/* From season */}
      <div className="flex flex-col gap-1">
        <label className="text-xs text-muted-foreground font-medium">From Season</label>
        <select
          className={selectClass}
          value={filter.fromSeason}
          onChange={handleFrom}
        >
          {SEASONS.map((y) => (
            <option key={y} value={y} disabled={y > filter.toSeason}>
              {y}-{String(y + 1).slice(2)}
            </option>
          ))}
        </select>
      </div>

      {/* To season */}
      <div className="flex flex-col gap-1">
        <label className="text-xs text-muted-foreground font-medium">To Season</label>
        <select
          className={selectClass}
          value={filter.toSeason}
          onChange={handleTo}
        >
          {SEASONS.map((y) => (
            <option key={y} value={y} disabled={y < filter.fromSeason}>
              {y}-{String(y + 1).slice(2)}
            </option>
          ))}
        </select>
      </div>

      {/* Reset */}
      <button
        onClick={onReset}
        className="rounded-md bg-muted px-3 py-1.5 text-sm font-medium text-foreground hover:bg-muted/80 transition-colors border border-border"
      >
        Reset
      </button>
    </div>
  )
}
