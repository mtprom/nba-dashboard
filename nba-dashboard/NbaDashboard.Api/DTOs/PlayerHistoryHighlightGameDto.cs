namespace NbaDashboard.Api.DTOs;

public class PlayerHistoryHighlightGameDto
{
    public string GameId { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public int SeasonYear { get; set; }
    public int TeamId { get; set; }
    public int OpponentTeamId { get; set; }
    public bool IsHome { get; set; }
    public bool Won { get; set; }
    public int HomeTeamId { get; set; }
    public int AwayTeamId { get; set; }
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public double Minutes { get; set; }
    public int Points { get; set; }
    public int Rebounds { get; set; }
    public int Assists { get; set; }
    public int PlusMinus { get; set; }
    public int FieldGoalsMade { get; set; }
    public int FieldGoalsAttempted { get; set; }
    public double FieldGoalPct { get; set; }
    public double? TsPct { get; set; }
}
