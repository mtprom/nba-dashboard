namespace NbaDashboard.Api.DTOs;

public class HotTeamsResponseDto
{
    public List<HotTeamDto> Hot { get; set; } = [];
    public List<HotTeamDto> Cold { get; set; } = [];
}
