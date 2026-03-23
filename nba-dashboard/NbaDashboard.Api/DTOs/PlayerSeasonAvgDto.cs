namespace NbaDashboard.Api.DTOs;

public class PlayerSeasonAvgDto
{
    public int PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string JerseyNumber { get; set; } = string.Empty;
    public int GamesPlayed { get; set; }
    public decimal PtsAvg { get; set; }
    public decimal RebAvg { get; set; }
    public decimal AstAvg { get; set; }
    public decimal StlAvg { get; set; }
    public decimal BlkAvg { get; set; }
    public decimal FgPct { get; set; }
    public decimal Fg3Pct { get; set; }
    public decimal TsPct { get; set; }
}
