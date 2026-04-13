import { Link, useLocation } from "react-router-dom"

export default function Header() {
  const location = useLocation()
  const isHistoryRoute = location.pathname === "/" || location.pathname === "/history"

  const navItems = [
    { label: "History", path: "/history" },
    { label: "Today's Games", path: "/games" },
    { label: "Hot", path: "/hot" },
    { label: "Standings", path: "/standings" },
  ]

  return (
    <header className="border-b border-border bg-card">
      <div className="mx-auto flex h-14 max-w-7xl items-center gap-8 px-4">
        <Link to="/" className="text-lg font-bold tracking-tight text-foreground">
          .netBall
        </Link>
        <nav className="flex items-center gap-1">
          {navItems.map((item) => (
            <Link
              key={item.path}
              to={item.path}
              className={`rounded-md px-3 py-1.5 text-sm font-medium transition-colors ${
                (item.path === "/history" && isHistoryRoute) || location.pathname === item.path
                  ? "bg-muted text-foreground"
                  : "text-muted-foreground hover:text-foreground hover:bg-muted/50"
              }`}
            >
              {item.label}
            </Link>
          ))}
        </nav>
      </div>
    </header>
  )
}
