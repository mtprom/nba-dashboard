namespace NbaDashboard.Core.Entities;

public class PlayerGameAdvanced
{
    public long Id { get; set; }
    public string GameId { get; set; } = string.Empty;
    public int PlayerId { get; set; }
    public int TeamId { get; set; }
    public decimal Minutes { get; set; }
    public decimal OffRating { get; set; }
    public decimal DefRating { get; set; }
    public decimal NetRating { get; set; }
    public decimal AstPct { get; set; }
    public decimal OrebPct { get; set; }
    public decimal DrebPct { get; set; }
    public decimal RebPct { get; set; }
    public decimal EfgPct { get; set; }
    public decimal TsPct { get; set; }
    public decimal UsgPct { get; set; }
    public decimal Pace { get; set; }
    public decimal Pie { get; set; }

    public Game Game { get; set; } = null!;
    public Player Player { get; set; } = null!;
    public Team Team { get; set; } = null!;
}
