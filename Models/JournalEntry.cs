using SQLite;
using System.Text.Json;

namespace journalstart.Models;

public class JournalEntry
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Date { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string? PrimaryMood { get; set; }

    // Store as JSON strings in SQLite
    public string SecondaryMoodsJson { get; set; } = "[]";

    public string TagsJson { get; set; } = "[]";

    public bool IsLocked { get; set; } = false;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Helper properties (not stored in DB)
    [Ignore]
    public DateOnly DateOnly
    {
        get => DateOnly.Parse(Date);
        set => Date = value.ToString("yyyy-MM-dd");
    }

    [Ignore]
    public List<string> SecondaryMoods
    {
        get => string.IsNullOrEmpty(SecondaryMoodsJson)
            ? new()
            : JsonSerializer.Deserialize<List<string>>(SecondaryMoodsJson) ?? new();
        set => SecondaryMoodsJson = JsonSerializer.Serialize(value);
    }

    [Ignore]
    public List<string> Tags
    {
        get => string.IsNullOrEmpty(TagsJson)
            ? new()
            : JsonSerializer.Deserialize<List<string>>(TagsJson) ?? new();
        set => TagsJson = JsonSerializer.Serialize(value);
    }
}
