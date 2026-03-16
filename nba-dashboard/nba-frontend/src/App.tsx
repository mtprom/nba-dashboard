import { Routes, Route } from "react-router-dom"
import GamePreviewPage from "@/pages/GamePreviewPage"

export default function App() {
  return (
    <div className="min-h-screen bg-background">
      <Routes>
        <Route path="/" element={<GamePreviewPage />} />
      </Routes>
    </div>
  )
}
