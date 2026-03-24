interface HeatBadgeProps {
  score: number
}

export default function HeatBadge({ score }: HeatBadgeProps) {
  const absScore = Math.abs(score)
  let bg: string
  let text: string

  if (score >= 0.3) {
    bg = "bg-red-500/20 text-red-400"
    text = "ON FIRE"
  } else if (score >= 0.1) {
    bg = "bg-orange-500/20 text-orange-400"
    text = "Hot"
  } else if (score > -0.1) {
    bg = "bg-zinc-500/20 text-zinc-400"
    text = "Neutral"
  } else if (score > -0.3) {
    bg = "bg-blue-400/20 text-blue-400"
    text = "Cold"
  } else {
    bg = "bg-blue-600/20 text-blue-300"
    text = "ICE COLD"
  }

  return (
    <span
      className={`inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-semibold ${bg}`}
    >
      {score > 0 ? "+" : ""}
      {score.toFixed(2)} {text}
    </span>
  )
}
