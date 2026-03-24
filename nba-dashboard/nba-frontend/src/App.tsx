import { Routes, Route } from "react-router-dom"
import GamePreviewPage from "@/pages/GamePreviewPage"
import StandingsPage from "@/pages/StandingsPage"
import HotPage from "@/pages/HotPage"

export default function App() {
  return (
    <div className="min-h-screen bg-background">
      <Routes>
        <Route path="/" element={<GamePreviewPage />} />
        <Route path="/standings" element={<StandingsPage />} />
        <Route path="/hot" element={<HotPage />} />
      </Routes>
    </div>
  )
}
