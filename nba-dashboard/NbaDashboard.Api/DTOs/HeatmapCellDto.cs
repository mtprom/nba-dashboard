namespace NbaDashboard.Api.DTOs;

public class HeatmapCellDto
{
    public int SeasonYear { get; set; }
    public int Month { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public double? WinPct { get; set; }
}
