namespace NbaDashboard.Api.DTOs;

public class MatchupHistoryDto
{
    public TeamDto Team { get; set; } = null!;
    public TeamDto Opponent { get; set; } = null!;
    public List<MatchupGameDto> Games { get; set; } = [];
    public int TeamWins { get; set; }
    public int OpponentWins { get; set; }
}
