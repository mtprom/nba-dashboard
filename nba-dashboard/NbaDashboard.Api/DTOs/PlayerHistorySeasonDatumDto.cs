namespace NbaDashboard.Api.DTOs;

public class PlayerHistorySeasonDatumDto
{
    public int SeasonYear { get; set; }
    public string SeasonLabel { get; set; } = string.Empty;
    public int GamesPlayed { get; set; }
    public double Minutes { get; set; }
    public double Points { get; set; }
    public double Rebounds { get; set; }
    public double Assists { get; set; }
    public double Steals { get; set; }
    public double Blocks { get; set; }
    public double Turnovers { get; set; }
    public double PlusMinus { get; set; }
    public double FieldGoalPct { get; set; }
    public double ThreePointPct { get; set; }
    public double FreeThrowPct { get; set; }
    public double? EfgPct { get; set; }
    public double? TsPct { get; set; }
    public double? NetRating { get; set; }
    public double? UsgPct { get; set; }
    public double? AstPct { get; set; }
    public double? RebPct { get; set; }
    public double? Pie { get; set; }
}
