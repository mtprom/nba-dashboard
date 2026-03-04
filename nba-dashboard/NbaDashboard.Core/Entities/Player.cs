namespace NbaDashboard.Core.Entities;

public class Player
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Height { get; set; } = string.Empty;
    public string Weight { get; set; } = string.Empty;
    public int TeamId { get; set; }
    public string JerseyNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Team Team { get; set; } = null!;
    public ICollection<PlayerSeasonStats> SeasonStats { get; set; } = new List<PlayerSeasonStats>();
    public ICollection<PlayerGameStats> GameStats { get; set; } = new List<PlayerGameStats>();
    public ICollection<PlayerGameAdvanced> GameAdvanced { get; set; } = new List<PlayerGameAdvanced>();
    public ICollection<PlayerHeat> HeatScores { get; set; } = new List<PlayerHeat>();
}
