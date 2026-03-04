namespace NbaDashboard.Core.Entities;

public class PlayerHeat
{
    public long Id { get; set; }
    public int PlayerId { get; set; }
    public DateTime ComputedDate { get; set; }
    public decimal HeatScore { get; set; }
    public int GamesSampled { get; set; }
    public decimal PtsAvg { get; set; }
    public decimal TsPctAvg { get; set; }
    public decimal UsgPctAvg { get; set; }
    public decimal NetRatingAvg { get; set; }
    public decimal PieAvg { get; set; }

    public Player Player { get; set; } = null!;
}
