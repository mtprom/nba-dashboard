namespace NbaDashboard.Api.DTOs;

public class HotPlayerDto
{
    public int PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string JerseyNumber { get; set; } = string.Empty;
    public TeamDto Team { get; set; } = null!;
    public decimal HeatScore { get; set; }
    public int GamesPlayed { get; set; }

    // Recent window averages
    public decimal PtsAvg { get; set; }
    public decimal RebAvg { get; set; }
    public decimal AstAvg { get; set; }
    public decimal FgPct { get; set; }
    public decimal TsPct { get; set; }
    public decimal NetRating { get; set; }

    // Baseline (season avg or last season)
    public decimal BaselinePtsAvg { get; set; }
    public decimal BaselineRebAvg { get; set; }
    public decimal BaselineAstAvg { get; set; }
    public decimal BaselineFgPct { get; set; }
    public decimal BaselineTsPct { get; set; }
    public decimal BaselineNetRating { get; set; }

    // Deltas
    public decimal PtsDelta { get; set; }
    public decimal RebDelta { get; set; }
    public decimal AstDelta { get; set; }
    public decimal FgPctDelta { get; set; }
    public decimal TsPctDelta { get; set; }
    public decimal NetRatingDelta { get; set; }
}
