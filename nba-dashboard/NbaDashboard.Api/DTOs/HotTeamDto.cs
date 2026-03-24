namespace NbaDashboard.Api.DTOs;

public class HotTeamDto
{
    public TeamDto Team { get; set; } = null!;
    public decimal HeatScore { get; set; }

    // Recent window
    public int WindowWins { get; set; }
    public int WindowLosses { get; set; }
    public decimal WindowWinPct { get; set; }
    public decimal WindowPtsScored { get; set; }
    public decimal WindowPtsAllowed { get; set; }
    public decimal WindowNetRating { get; set; }

    // Baseline (season or last season)
    public decimal BaselineWinPct { get; set; }
    public decimal BaselineOffRating { get; set; }
    public decimal BaselineDefRating { get; set; }
    public decimal BaselineNetRating { get; set; }

    // Deltas
    public decimal WinPctDelta { get; set; }
    public decimal ScoringDelta { get; set; }
    public decimal NetRatingDelta { get; set; }
}
