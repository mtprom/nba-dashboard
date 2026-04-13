namespace NbaDashboard.Api.DTOs;

public class PlayerHistoryGameDto
{
    public string GameId { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public int SeasonYear { get; set; }
    public int TeamId { get; set; }
    public int OpponentTeamId { get; set; }
    public bool IsHome { get; set; }
    public bool Won { get; set; }
    public int HomeTeamId { get; set; }
    public int AwayTeamId { get; set; }
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public string StartPosition { get; set; } = string.Empty;
    public double Minutes { get; set; }
    public int Points { get; set; }
    public int Rebounds { get; set; }
    public int Assists { get; set; }
    public int Steals { get; set; }
    public int Blocks { get; set; }
    public int Turnovers { get; set; }
    public int PersonalFouls { get; set; }
    public int PlusMinus { get; set; }
    public int FieldGoalsMade { get; set; }
    public int FieldGoalsAttempted { get; set; }
    public double FieldGoalPct { get; set; }
    public int ThreePointersMade { get; set; }
    public int ThreePointersAttempted { get; set; }
    public double ThreePointPct { get; set; }
    public int FreeThrowsMade { get; set; }
    public int FreeThrowsAttempted { get; set; }
    public double FreeThrowPct { get; set; }
    public int OffensiveRebounds { get; set; }
    public int DefensiveRebounds { get; set; }
    public double? OffRating { get; set; }
    public double? DefRating { get; set; }
    public double? NetRating { get; set; }
    public double? AstPct { get; set; }
    public double? OrebPct { get; set; }
    public double? DrebPct { get; set; }
    public double? RebPct { get; set; }
    public double? EfgPct { get; set; }
    public double? TsPct { get; set; }
    public double? UsgPct { get; set; }
    public double? Pace { get; set; }
    public double? Pie { get; set; }
}
