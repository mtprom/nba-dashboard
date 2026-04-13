namespace NbaDashboard.Api.DTOs;

public class PlayerHistoryHighlightsDto
{
    public PlayerHistoryHighlightGameDto? HighestPoints { get; set; }
    public PlayerHistoryHighlightGameDto? HighestRebounds { get; set; }
    public PlayerHistoryHighlightGameDto? HighestAssists { get; set; }
    public PlayerHistoryHighlightGameDto? BestEfficiency { get; set; }
    public PlayerHistoryHighlightGameDto? WorstShooting { get; set; }
    public PlayerHistoryHighlightGameDto? BestPlusMinus { get; set; }
    public PlayerHistoryHighlightGameDto? WorstPlusMinus { get; set; }
}
