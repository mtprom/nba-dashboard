namespace NbaDashboard.Api.DTOs;

public class LeaguePlacementDto
{
    public int SeasonYear { get; set; }
    public string SeasonLabel { get; set; } = string.Empty;
    public int SelectedTeamId { get; set; }
    public int SelectedLeagueRank { get; set; }
    public int SelectedConferenceRank { get; set; }
    public string SelectedConference { get; set; } = string.Empty;
    public List<LeaguePlacementTeamDto> Teams { get; set; } = [];
}
