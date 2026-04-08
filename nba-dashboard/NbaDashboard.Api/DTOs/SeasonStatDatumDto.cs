namespace NbaDashboard.Api.DTOs;

public class SeasonStatDatumDto
{
    public int SeasonYear { get; set; }
    public string SeasonLabel { get; set; } = string.Empty;
    public int GameCount { get; set; }

    // Team mode only (null when all teams)
    public double? WinPct { get; set; }
    public double? HomeWinPct { get; set; }
    public double? AwayWinPct { get; set; }

    // All-teams mode only (null when team selected)
    public double? AvgTotalPoints { get; set; }
    public double? LeagueHomeWinPct { get; set; }

    // Both modes
    public int CloseGames { get; set; }    // margin 1–5
    public int ModerateGames { get; set; } // margin 6–19
    public int BlowoutGames { get; set; }  // margin >= 20
}
