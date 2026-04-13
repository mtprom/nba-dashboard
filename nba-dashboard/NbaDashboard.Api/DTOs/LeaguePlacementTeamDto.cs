namespace NbaDashboard.Api.DTOs;

public class LeaguePlacementTeamDto
{
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public string Abbreviation { get; set; } = string.Empty;
    public string Conference { get; set; } = string.Empty;
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int GameCount { get; set; }
    public double WinPct { get; set; }
    public int LeagueRank { get; set; }
    public int ConferenceRank { get; set; }
}
