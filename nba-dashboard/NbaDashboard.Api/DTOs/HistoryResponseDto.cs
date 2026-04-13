namespace NbaDashboard.Api.DTOs;

public class HistoryResponseDto
{
    public HistoryMetricsDto Metrics { get; set; } = null!;
    public List<SeasonBarDatumDto> SeasonBarData { get; set; } = [];
    public List<HeatmapCellDto> HeatmapData { get; set; } = [];
    public List<HistoryGameDto> ClosestGames { get; set; } = [];
    public List<HistoryGameDto> BlowoutGames { get; set; } = [];
    public List<SeasonStatDatumDto> SeasonStats { get; set; } = [];
    public BestWorstGamesDto? BestWorstGames { get; set; }
    public LeaguePlacementDto? LeaguePlacement { get; set; }
}
