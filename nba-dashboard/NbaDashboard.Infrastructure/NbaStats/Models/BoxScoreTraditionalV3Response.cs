namespace NbaDashboard.Infrastructure.NbaStats.Models;

// boxscoretraditionalv3 response shape from real API:
// { "meta": {...}, "boxScoreTraditional": { "gameId": "...", "homeTeamId": int, "awayTeamId": int,
//   "homeTeam": { teamId, teamCity, teamName, teamTricode, players: [...] },
//   "awayTeam": { ... } } }
// minutes field is "MM:SS" string e.g. "35:56"

public class BoxScoreTraditionalV3Response
{
    public BoxScoreTraditional BoxScoreTraditional { get; set; } = null!;
}

public class BoxScoreTraditional
{
    public string GameId { get; set; } = string.Empty;
    public int AwayTeamId { get; set; }
    public int HomeTeamId { get; set; }
    public TraditionalTeam HomeTeam { get; set; } = null!;
    public TraditionalTeam AwayTeam { get; set; } = null!;
}

public class TraditionalTeam
{
    public int TeamId { get; set; }
    public string TeamCity { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string TeamTricode { get; set; } = string.Empty;
    public List<TraditionalPlayer> Players { get; set; } = [];
}

public class TraditionalPlayer
{
    public int PersonId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string JerseyNum { get; set; } = string.Empty;
    public TraditionalStatistics Statistics { get; set; } = null!;
}

public class TraditionalStatistics
{
    // "MM:SS" e.g. "35:56"
    public string Minutes { get; set; } = string.Empty;
    public int FieldGoalsMade { get; set; }
    public int FieldGoalsAttempted { get; set; }
    public double FieldGoalsPercentage { get; set; }
    public int ThreePointersMade { get; set; }
    public int ThreePointersAttempted { get; set; }
    public double ThreePointersPercentage { get; set; }
    public int FreeThrowsMade { get; set; }
    public int FreeThrowsAttempted { get; set; }
    public double FreeThrowsPercentage { get; set; }
    public int ReboundsOffensive { get; set; }
    public int ReboundsDefensive { get; set; }
    public int ReboundsTotal { get; set; }
    public int Assists { get; set; }
    public int Steals { get; set; }
    public int Blocks { get; set; }
    public int Turnovers { get; set; }
    public int FoulsPersonal { get; set; }
    public int Points { get; set; }
    public double PlusMinusPoints { get; set; }

    /// <summary>Converts "MM:SS" to decimal minutes.</summary>
    public decimal MinutesDecimal()
    {
        if (string.IsNullOrEmpty(Minutes)) return 0;
        var parts = Minutes.Split(':');
        if (parts.Length != 2) return 0;
        return decimal.Parse(parts[0]) + decimal.Parse(parts[1]) / 60m;
    }
}
