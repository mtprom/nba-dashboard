import type {
  PlayerHistoryGame,
  PlayerHistoryHighlightGame,
  PlayerHistoryMetricKey,
  PlayerHistorySeasonDatum,
  PlayerHistorySplit,
} from "@/types/player-history"

type MetricFormat = "number" | "percent" | "rating"

interface MetricDef {
  label: string
  shortLabel: string
  format: MetricFormat
}

export const PLAYER_HISTORY_METRICS: Record<PlayerHistoryMetricKey, MetricDef> = {
  points: { label: "Points", shortLabel: "PTS", format: "number" },
  rebounds: { label: "Rebounds", shortLabel: "REB", format: "number" },
  assists: { label: "Assists", shortLabel: "AST", format: "number" },
  minutes: { label: "Minutes", shortLabel: "MIN", format: "number" },
  fieldGoalPct: { label: "Field Goal %", shortLabel: "FG%", format: "percent" },
  threePointPct: { label: "Three Point %", shortLabel: "3P%", format: "percent" },
  freeThrowPct: { label: "Free Throw %", shortLabel: "FT%", format: "percent" },
  tsPct: { label: "True Shooting %", shortLabel: "TS%", format: "percent" },
  efgPct: { label: "Effective FG %", shortLabel: "eFG%", format: "percent" },
  netRating: { label: "Net Rating", shortLabel: "NET RTG", format: "rating" },
  usgPct: { label: "Usage %", shortLabel: "USG%", format: "percent" },
  astPct: { label: "Assist %", shortLabel: "AST%", format: "percent" },
  rebPct: { label: "Rebound %", shortLabel: "REB%", format: "percent" },
  pie: { label: "PIE", shortLabel: "PIE", format: "percent" },
}

type MetricSource = PlayerHistoryGame | PlayerHistorySeasonDatum | PlayerHistorySplit | PlayerHistoryHighlightGame

export function getMetricValue(source: MetricSource, metric: PlayerHistoryMetricKey): number | null {
  return source[metric] ?? null
}

export function formatMetricValue(metric: PlayerHistoryMetricKey, value: number | null | undefined): string {
  if (value === null || value === undefined || Number.isNaN(value)) return "—"

  const def = PLAYER_HISTORY_METRICS[metric]
  switch (def.format) {
    case "percent":
      return `${(value * 100).toFixed(1)}%`
    case "rating":
      return value.toFixed(1)
    case "number":
    default:
      return value.toFixed(1)
  }
}

export function metricAxisLabel(metric: PlayerHistoryMetricKey): string {
  return PLAYER_HISTORY_METRICS[metric].shortLabel
}

export function metricLabel(metric: PlayerHistoryMetricKey): string {
  return PLAYER_HISTORY_METRICS[metric].label
}

export function formatPlayerHistoryDate(iso: string): string {
  return new Date(`${iso}T12:00:00`).toLocaleDateString("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric",
  })
}

export function seasonRange(first: number, last: number): number[] {
  return Array.from({ length: last - first + 1 }, (_, i) => first + i)
}
