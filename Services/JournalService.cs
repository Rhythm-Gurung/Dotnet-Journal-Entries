using journalstart.Models;

namespace journalstart.Services;

public class JournalService
{
    private readonly Dictionary<DateOnly, JournalEntry> _entries = new();
    private readonly object _lock = new();

    public Task<JournalEntry?> GetEntryAsync(DateOnly date)
    {
        lock (_lock)
        {
            _entries.TryGetValue(date, out var entry);
            return Task.FromResult(entry);
        }
    }

    public Task<JournalEntry> UpsertEntryAsync(DateOnly date, string content, string? primaryMood, List<string> secondaryMoods, List<string> tags)
    {
        var now = DateTime.UtcNow;
        lock (_lock)
        {
            if (_entries.TryGetValue(date, out var existing))
            {
                existing.Content = content;
                existing.PrimaryMood = primaryMood;
                existing.SecondaryMoods = secondaryMoods;
                existing.Tags = tags;
                existing.UpdatedAt = now;
                return Task.FromResult(existing);
            }

            var entry = new JournalEntry
            {
                Date = date,
                Content = content,
                PrimaryMood = primaryMood,
                SecondaryMoods = secondaryMoods,
                Tags = tags,
                CreatedAt = now,
                UpdatedAt = now
            };

            _entries[date] = entry;
            return Task.FromResult(entry);
        }
    }

    public Task<bool> DeleteEntryAsync(DateOnly date)
    {
        lock (_lock)
        {
            return Task.FromResult(_entries.Remove(date));
        }
    }

    public Task<List<DateOnly>> GetEntryDatesAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_entries.Keys.ToList());
        }
    }

    public Task<List<JournalEntry>> GetAllEntriesAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_entries.Values.OrderByDescending(e => e.Date).ToList());
        }
    }
}
