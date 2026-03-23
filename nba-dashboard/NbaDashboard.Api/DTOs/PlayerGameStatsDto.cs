namespace NbaDashboard.Api.DTOs;

public class PlayerGameStatsDto
{
    public int PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string JerseyNumber { get; set; } = string.Empty;
    public decimal Minutes { get; set; }
    public int Points { get; set; }
    public int Rebounds { get; set; }
    public int Assists { get; set; }
    public int Steals { get; set; }
    public int Blocks { get; set; }
    public int Turnovers { get; set; }
    public decimal FieldGoalPct { get; set; }
    public decimal ThreePointPct { get; set; }
    public decimal FreeThrowPct { get; set; }
    public int PlusMinus { get; set; }
}
