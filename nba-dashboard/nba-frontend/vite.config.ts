import dns from "node:dns"
import path from "path"
import react from "@vitejs/plugin-react"
import { defineConfig } from "vite"

// Force IPv4 lookup — Vite's http-proxy fails with EHOSTUNREACH on some networks
dns.setDefaultResultOrder("ipv4first")

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  server: {
    proxy: {
      "/api": {
        target: process.env.API_URL ?? "http://192.168.1.150:5000",
        changeOrigin: true,
        configure: (proxy) => {
          proxy.on("error", (err, _req, res) => {
            console.error("[proxy error]", err.message)
            if ("headersSent" in res && !res.headersSent) {
              res.writeHead(502, { "Content-Type": "application/json" })
              res.end(JSON.stringify({ error: "Proxy error: " + err.message }))
            }
          })
        },
      },
    },
  },
})
