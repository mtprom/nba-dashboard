namespace NbaDashboard.Api.DTOs;

public class PlayerHistoryMetricsDto
{
    public int TotalGames { get; set; }
    public int SeasonsCovered { get; set; }
    public double AvgMinutes { get; set; }
    public double AvgPoints { get; set; }
    public double AvgRebounds { get; set; }
    public double AvgAssists { get; set; }
}
