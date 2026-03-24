import path from "path"
import react from "@vitejs/plugin-react"
import { defineConfig } from "vite"

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  server: {
    allowedHosts: ["nba.mtprom.dev"],
    proxy: {
      "/api": {
        target: "http://api:8080",
        changeOrigin: true,
      },
    },
  },
})
