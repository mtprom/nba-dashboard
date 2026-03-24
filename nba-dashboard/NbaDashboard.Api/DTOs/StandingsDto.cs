namespace NbaDashboard.Api.DTOs;

public class StandingsDto
{
    public TeamDto Team { get; set; } = null!;
    public string Conference { get; set; } = string.Empty;
    public int Wins { get; set; }
    public int Losses { get; set; }
    public decimal WinPct { get; set; }
    public int ConfRank { get; set; }
    public int DivRank { get; set; }
    public string HomeRecord { get; set; } = string.Empty;
    public string AwayRecord { get; set; } = string.Empty;
    public string Last10 { get; set; } = string.Empty;
    public string Streak { get; set; } = string.Empty;
    public decimal OffRating { get; set; }
    public decimal DefRating { get; set; }
    public decimal NetRating { get; set; }
    public decimal Pace { get; set; }
}
