import type { ReactNode } from "react"

export default function PageContainer({ children }: { children: ReactNode }) {
  return (
    <main className="mx-auto max-w-7xl px-4 py-6">
      {children}
    </main>
  )
}
