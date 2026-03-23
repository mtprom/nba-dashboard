namespace NbaDashboard.Api.DTOs;

public class UpcomingGameDto
{
    public GameDto Game { get; set; } = null!;
    public TeamDto HomeTeam { get; set; } = null!;
    public TeamDto VisitorTeam { get; set; } = null!;
}
