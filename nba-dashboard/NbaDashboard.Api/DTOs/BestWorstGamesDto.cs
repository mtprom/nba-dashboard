namespace NbaDashboard.Api.DTOs;

public class BestWorstGamesDto
{
    public HistoryGameDto? LargestWin { get; set; }
    public HistoryGameDto? LargestLoss { get; set; }
    public HistoryGameDto? HighestScoringGame { get; set; }
    public HistoryGameDto? LowestScoringGame { get; set; }
}
