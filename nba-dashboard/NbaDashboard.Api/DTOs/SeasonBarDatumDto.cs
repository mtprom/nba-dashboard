namespace NbaDashboard.Api.DTOs;

public class SeasonBarDatumDto
{
    public int SeasonYear { get; set; }
    public string SeasonLabel { get; set; } = string.Empty;
    public int GameCount { get; set; }
    public string? Annotation { get; set; }
}
