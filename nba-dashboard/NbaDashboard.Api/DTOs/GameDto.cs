namespace NbaDashboard.Api.DTOs;

public class GameDto
{
    public string Id { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int HomeTeamId { get; set; }
    public int VisitorTeamId { get; set; }
    public int HomeScore { get; set; }
    public int VisitorScore { get; set; }
    public string Arena { get; set; } = string.Empty;
}
