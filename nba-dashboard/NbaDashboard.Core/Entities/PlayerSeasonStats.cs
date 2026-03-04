namespace NbaDashboard.Core.Entities;

public class PlayerSeasonStats
{
    public long Id { get; set; }
    public int PlayerId { get; set; }
    public int SeasonId { get; set; }
    public int TeamId { get; set; }
    public int GamesPlayed { get; set; }
    public decimal MinAvg { get; set; }
    public decimal PtsAvg { get; set; }
    public decimal RebAvg { get; set; }
    public decimal AstAvg { get; set; }
    public decimal StlAvg { get; set; }
    public decimal BlkAvg { get; set; }
    public decimal ToAvg { get; set; }
    public decimal FgPct { get; set; }
    public decimal Fg3Pct { get; set; }
    public decimal FtPct { get; set; }
    public decimal TsPct { get; set; }
    public decimal UsgPct { get; set; }
    public decimal NetRating { get; set; }
    public decimal Pie { get; set; }
    public decimal Per { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Player Player { get; set; } = null!;
    public Season Season { get; set; } = null!;
    public Team Team { get; set; } = null!;
}
