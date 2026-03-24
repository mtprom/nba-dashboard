import { Routes, Route } from "react-router-dom"
import GamePreviewPage from "@/pages/GamePreviewPage"
import StandingsPage from "@/pages/StandingsPage"

export default function App() {
  return (
    <div className="min-h-screen bg-background">
      <Routes>
        <Route path="/" element={<GamePreviewPage />} />
        <Route path="/standings" element={<StandingsPage />} />
      </Routes>
    </div>
  )
}
