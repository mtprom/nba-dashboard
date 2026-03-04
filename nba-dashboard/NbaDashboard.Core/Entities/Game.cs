namespace NbaDashboard.Core.Entities;

public class Game
{
    public string Id { get; set; } = string.Empty;
    public int SeasonId { get; set; }
    public DateTime Date { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Period { get; set; }
    public string TimeRemaining { get; set; } = string.Empty;
    public int HomeTeamId { get; set; }
    public int VisitorTeamId { get; set; }
    public int HomeScore { get; set; }
    public int VisitorScore { get; set; }
    public bool Postseason { get; set; }
    public string Arena { get; set; } = string.Empty;
    public DateTime IngestedAt { get; set; } = DateTime.UtcNow;

    public Season Season { get; set; } = null!;
    public Team HomeTeam { get; set; } = null!;
    public Team VisitorTeam { get; set; } = null!;
    public ICollection<PlayerGameStats> PlayerGameStats { get; set; } = new List<PlayerGameStats>();
    public ICollection<PlayerGameAdvanced> PlayerGameAdvanced { get; set; } = new List<PlayerGameAdvanced>();
}
