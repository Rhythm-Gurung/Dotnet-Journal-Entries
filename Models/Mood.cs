namespace journalstart.Models;

public enum MoodCategory
{
    Positive,
    Neutral,
    Negative
}

public record Mood(string Id, string Name, string Emoji, MoodCategory Category);

public static class MoodCatalog
{
    public static readonly IReadOnlyList<Mood> All = new List<Mood>
    {
        // Positive moods
        new("happy", "Happy", "😊", MoodCategory.Positive),
        new("excited", "Excited", "🤩", MoodCategory.Positive),
        new("grateful", "Grateful", "🙏", MoodCategory.Positive),
        new("calm", "Calm", "😌", MoodCategory.Positive),
        new("hopeful", "Hopeful", "🌟", MoodCategory.Positive),
        new("loved", "Loved", "🥰", MoodCategory.Positive),
        new("proud", "Proud", "😤", MoodCategory.Positive),
        new("inspired", "Inspired", "✨", MoodCategory.Positive),
        new("energetic", "Energetic", "⚡", MoodCategory.Positive),
        new("playful", "Playful", "😜", MoodCategory.Positive),

        // Neutral moods
        new("focused", "Focused", "🎯", MoodCategory.Neutral),
        new("thoughtful", "Thoughtful", "🤔", MoodCategory.Neutral),
        new("tired", "Tired", "😴", MoodCategory.Neutral),
        new("busy", "Busy", "🏃", MoodCategory.Neutral),
        new("relaxed", "Relaxed", "😎", MoodCategory.Neutral),
        new("curious", "Curious", "🧐", MoodCategory.Neutral),
        new("meh", "Meh", "😐", MoodCategory.Neutral),
        new("nostalgic", "Nostalgic", "🥹", MoodCategory.Neutral),
        new("restless", "Restless", "😶‍🌫️", MoodCategory.Neutral),
        new("reflective", "Reflective", "🪞", MoodCategory.Neutral),

        // Negative moods
        new("sad", "Sad", "😔", MoodCategory.Negative),
        new("anxious", "Anxious", "😬", MoodCategory.Negative),
        new("stressed", "Stressed", "😫", MoodCategory.Negative),
        new("angry", "Angry", "😠", MoodCategory.Negative),
        new("frustrated", "Frustrated", "😤", MoodCategory.Negative),
        new("lonely", "Lonely", "🥺", MoodCategory.Negative),
        new("overwhelmed", "Overwhelmed", "😵", MoodCategory.Negative),
        new("disappointed", "Disappointed", "😞", MoodCategory.Negative),
        new("jealous", "Jealous", "😒", MoodCategory.Negative),
        new("guilty", "Guilty", "😔", MoodCategory.Negative),
    };

    public static readonly IReadOnlyDictionary<MoodCategory, IReadOnlyList<Mood>> ByCategory =
        All.GroupBy(m => m.Category)
           .ToDictionary(g => g.Key, g => (IReadOnlyList<Mood>)g.ToList());

    public static readonly IReadOnlyDictionary<string, Mood> ById =
        All.ToDictionary(m => m.Id);

    public static Mood? Get(string? id) => id is not null && ById.TryGetValue(id, out var mood) ? mood : null;
}
