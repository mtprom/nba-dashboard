namespace NbaDashboard.Api.DTOs;

public class MatchupGameDto
{
    public GameDto Game { get; set; } = null!;
    public TeamDto HomeTeam { get; set; } = null!;
    public TeamDto VisitorTeam { get; set; } = null!;
    public List<PlayerGameStatsDto> HomePlayerStats { get; set; } = [];
    public List<PlayerGameStatsDto> VisitorPlayerStats { get; set; } = [];
}
