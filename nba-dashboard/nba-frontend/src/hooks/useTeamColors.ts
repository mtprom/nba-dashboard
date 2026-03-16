import { useEffect, type RefObject } from "react"
import { getTeamColors } from "@/data/teams"

export function useTeamColors(
  homeTeamId: number,
  visitorTeamId: number,
  ref?: RefObject<HTMLElement | null>
) {
  useEffect(() => {
    const el = ref?.current ?? document.documentElement
    const home = getTeamColors(homeTeamId)
    const away = getTeamColors(visitorTeamId)

    el.style.setProperty("--team-home-primary", home.primary)
    el.style.setProperty("--team-home-secondary", home.secondary)
    el.style.setProperty("--team-away-primary", away.primary)
    el.style.setProperty("--team-away-secondary", away.secondary)

    return () => {
      el.style.removeProperty("--team-home-primary")
      el.style.removeProperty("--team-home-secondary")
      el.style.removeProperty("--team-away-primary")
      el.style.removeProperty("--team-away-secondary")
    }
  }, [homeTeamId, visitorTeamId, ref])
}
