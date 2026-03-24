interface DeltaStatProps {
  value: number
  baseline: number
  delta: number
  label: string
  isPct?: boolean
}

export default function DeltaStat({ value, delta, label, isPct }: DeltaStatProps) {
  const isPositive = delta > 0
  const isNegative = delta < 0
  const color = isPositive
    ? "text-green-500"
    : isNegative
      ? "text-red-500"
      : "text-muted-foreground"

  const formatted = isPct
    ? (value * 100).toFixed(1) + "%"
    : value.toFixed(1)

  const deltaFormatted = isPct
    ? (delta > 0 ? "+" : "") + (delta * 100).toFixed(1) + "%"
    : (delta > 0 ? "+" : "") + delta.toFixed(1)

  return (
    <div className="text-center">
      <div className="text-xs text-muted-foreground">{label}</div>
      <div className="font-medium">{formatted}</div>
      <div className={`text-xs font-medium ${color}`}>
        {isPositive && "\u2191"}
        {isNegative && "\u2193"}
        {deltaFormatted}
      </div>
    </div>
  )
}
