namespace NbaDashboard.Api.DTOs;

public class HistoryGameDto
{
    public string Id { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public int SeasonYear { get; set; }
    public int Month { get; set; }
    public int HomeTeamId { get; set; }
    public int AwayTeamId { get; set; }
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
}
