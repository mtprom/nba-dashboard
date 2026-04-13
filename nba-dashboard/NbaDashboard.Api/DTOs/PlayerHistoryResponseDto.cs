namespace NbaDashboard.Api.DTOs;

public class PlayerHistoryResponseDto
{
    public PlayerHistoryPlayerDto Player { get; set; } = new();
    public List<int> AvailableSeasonYears { get; set; } = [];
    public PlayerHistoryMetricsDto Metrics { get; set; } = new();
    public List<PlayerHistoryGameDto> GameLog { get; set; } = [];
    public List<PlayerHistorySeasonDatumDto> SeasonStats { get; set; } = [];
    public List<PlayerHistorySplitDto> HomeAwaySplits { get; set; } = [];
    public List<PlayerHistorySplitDto> WinLossSplits { get; set; } = [];
    public PlayerHistoryHighlightsDto Highlights { get; set; } = new();
}
