namespace journalstart.Models;

public class JournalEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateOnly Date { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? PrimaryMood { get; set; }
    public List<string> SecondaryMoods { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
