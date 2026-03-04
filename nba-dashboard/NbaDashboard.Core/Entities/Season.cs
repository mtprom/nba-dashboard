namespace NbaDashboard.Core.Entities;

public class Season
{
    public int Id { get; set; }
    public int Year { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Game> Games { get; set; } = new List<Game>();
}
