namespace NbaDashboard.Core.Entities;

public class PlayerGameStats
{
    public long Id { get; set; }
    public string GameId { get; set; } = string.Empty;
    public int PlayerId { get; set; }
    public int TeamId { get; set; }
    public string StartPosition { get; set; } = string.Empty;
    public decimal Minutes { get; set; }
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
    public decimal FieldGoalPct { get; set; }
    public int ThreePointersMade { get; set; }
    public int ThreePointersAttempted { get; set; }
    public decimal ThreePointPct { get; set; }
    public int FreeThrowsMade { get; set; }
    public int FreeThrowsAttempted { get; set; }
    public decimal FreeThrowPct { get; set; }
    public int OffensiveRebounds { get; set; }
    public int DefensiveRebounds { get; set; }

    public Game Game { get; set; } = null!;
    public Player Player { get; set; } = null!;
    public Team Team { get; set; } = null!;
}
