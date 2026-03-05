namespace NbaDashboard.Infrastructure.NbaStats.Models;

// boxscoreadvancedv3 response shape from real API:
// { "meta": {...}, "boxScoreAdvanced": { "gameId": "...", "homeTeamId": int, "awayTeamId": int,
//   "homeTeam": { teamId, ..., players: [...] }, "awayTeam": { ... } } }
// Note: PIE is all-caps in the JSON payload.

public class BoxScoreAdvancedV3Response
{
    public BoxScoreAdvanced BoxScoreAdvanced { get; set; } = null!;
}

public class BoxScoreAdvanced
{
    public string GameId { get; set; } = string.Empty;
    public int AwayTeamId { get; set; }
    public int HomeTeamId { get; set; }
    public AdvancedTeam HomeTeam { get; set; } = null!;
    public AdvancedTeam AwayTeam { get; set; } = null!;
}

public class AdvancedTeam
{
    public int TeamId { get; set; }
    public List<AdvancedPlayer> Players { get; set; } = [];
}

public class AdvancedPlayer
{
    public int PersonId { get; set; }
    public AdvancedStatistics Statistics { get; set; } = null!;
}

public class AdvancedStatistics
{
    // "MM:SS" e.g. "35:56"
    public string Minutes { get; set; } = string.Empty;
    public double OffensiveRating { get; set; }
    public double DefensiveRating { get; set; }
    public double NetRating { get; set; }
    public double AssistPercentage { get; set; }
    public double OffensiveReboundPercentage { get; set; }
    public double DefensiveReboundPercentage { get; set; }
    public double ReboundPercentage { get; set; }
    public double EffectiveFieldGoalPercentage { get; set; }
    public double TrueShootingPercentage { get; set; }
    public double UsagePercentage { get; set; }
    public double Pace { get; set; }
    // API returns "PIE" (all-caps); PropertyNameCaseInsensitive handles this.
    public double PIE { get; set; }

    /// <summary>Converts "MM:SS" to decimal minutes.</summary>
    public decimal MinutesDecimal()
    {
        if (string.IsNullOrEmpty(Minutes)) return 0;
        var parts = Minutes.Split(':');
        if (parts.Length != 2) return 0;
        return decimal.Parse(parts[0]) + decimal.Parse(parts[1]) / 60m;
    }
}
