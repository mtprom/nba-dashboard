import http from "node:http"
import path from "path"
import react from "@vitejs/plugin-react"
import { defineConfig } from "vite"
import type { Connect } from "vite"

const API_TARGET = "http://192.168.1.150:5000"

// Custom proxy using Node's native http.request (proven to work)
// Vite's built-in http-proxy fails with EHOSTUNREACH on this network
function apiProxy(): Connect.NextHandleFunction {
  return (req, res, next) => {
    if (!req.url?.startsWith("/api")) return next()

    const opts: http.RequestOptions = {
      hostname: "192.168.1.150",
      port: 5000,
      path: req.url,
      method: req.method,
      headers: { ...req.headers, host: "192.168.1.150:5000" },
    }

    const proxyReq = http.request(opts, (proxyRes) => {
      res.writeHead(proxyRes.statusCode ?? 500, proxyRes.headers)
      proxyRes.pipe(res)
    })

    proxyReq.on("error", (err) => {
      console.error("[proxy error]", err.message)
      if (!res.headersSent) {
        res.writeHead(502, { "Content-Type": "application/json" })
        res.end(JSON.stringify({ error: err.message }))
      }
    })

    req.pipe(proxyReq)
  }
}

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  server: {
    proxy: {},
  },
})

export { apiProxy }
