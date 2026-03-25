namespace NbaDashboard.Api.DTOs;

public class HotPlayersResponseDto
{
    public List<HotPlayerDto> Hot { get; set; } = [];
    public List<HotPlayerDto> Cold { get; set; } = [];
}
