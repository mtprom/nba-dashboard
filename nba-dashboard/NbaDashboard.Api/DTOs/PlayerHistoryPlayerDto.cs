namespace NbaDashboard.Api.DTOs;

public class PlayerHistoryPlayerDto
{
    public int PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string JerseyNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int? CurrentTeamId { get; set; }
    public string CurrentTeamName { get; set; } = string.Empty;
    public string CurrentTeamAbbreviation { get; set; } = string.Empty;
    public int FirstSeasonYear { get; set; }
    public int LastSeasonYear { get; set; }
}
