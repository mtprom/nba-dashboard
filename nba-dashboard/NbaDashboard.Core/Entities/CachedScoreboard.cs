namespace NbaDashboard.Core.Entities;

public class CachedScoreboard
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime FetchedAt { get; set; }
}
