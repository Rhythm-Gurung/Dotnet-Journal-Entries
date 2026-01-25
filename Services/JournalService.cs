using journalstart.Models;
using SQLite;

namespace journalstart.Services;

public class JournalService
{
    private readonly SQLiteAsyncConnection _database;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public JournalService()
    {
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "journal.db");
        _database = new SQLiteAsyncConnection(dbPath);
    }

    private async Task InitializeAsync()
    {
        if (_initialized) return;

        await _initLock.WaitAsync();
        try
        {
            if (!_initialized)
            {
                await _database.CreateTableAsync<JournalEntry>();
                _initialized = true;
            }
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<JournalEntry?> GetEntryAsync(DateOnly date)
    {
        await InitializeAsync();
        var dateStr = date.ToString("yyyy-MM-dd");
        return await _database.Table<JournalEntry>()
            .Where(e => e.Date == dateStr)
            .FirstOrDefaultAsync();
    }

    public async Task<JournalEntry> UpsertEntryAsync(DateOnly date, string content, string? primaryMood, List<string> secondaryMoods, List<string> tags)
    {
        await InitializeAsync();
        var now = DateTime.UtcNow;
        var dateStr = date.ToString("yyyy-MM-dd");

        var existing = await _database.Table<JournalEntry>()
            .Where(e => e.Date == dateStr)
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            existing.Content = content;
            existing.PrimaryMood = primaryMood;
            existing.SecondaryMoods = secondaryMoods;
            existing.Tags = tags;
            existing.UpdatedAt = now;
            await _database.UpdateAsync(existing);
            return existing;
        }

        var entry = new JournalEntry
        {
            DateOnly = date,
            Content = content,
            PrimaryMood = primaryMood,
            SecondaryMoods = secondaryMoods,
            Tags = tags,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _database.InsertAsync(entry);
        return entry;
    }

    public async Task<bool> DeleteEntryAsync(DateOnly date)
    {
        await InitializeAsync();
        var dateStr = date.ToString("yyyy-MM-dd");
        var entry = await _database.Table<JournalEntry>()
            .Where(e => e.Date == dateStr)
            .FirstOrDefaultAsync();

        if (entry != null)
        {
            await _database.DeleteAsync(entry);
            return true;
        }
        return false;
    }

    public async Task<List<DateOnly>> GetEntryDatesAsync()
    {
        await InitializeAsync();
        var entries = await _database.Table<JournalEntry>().ToListAsync();
        return entries.Select(e => e.DateOnly).ToList();
    }

    public async Task<List<JournalEntry>> GetAllEntriesAsync()
    {
        await InitializeAsync();
        var entries = await _database.Table<JournalEntry>().ToListAsync();
        return entries.OrderByDescending(e => e.DateOnly).ToList();
    }

    public async Task<bool> UpdateLockStatusAsync(string entryId, bool isLocked)
    {
        await InitializeAsync();
        var entry = await _database.Table<JournalEntry>()
            .Where(e => e.Id == entryId)
            .FirstOrDefaultAsync();

        if (entry != null)
        {
            entry.IsLocked = isLocked;
            entry.UpdatedAt = DateTime.UtcNow;
            await _database.UpdateAsync(entry);
            return true;
        }
        return false;
    }
}
