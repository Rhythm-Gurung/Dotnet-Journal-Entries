using journalstart.Models;
using journalstart.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace journalstart.Components.Pages;

public partial class Today
{
    [Inject] private JournalService JournalService { get; set; } = default!;

    private DateOnly SelectedDate = DateOnly.FromDateTime(DateTime.Now);
    private string Content = string.Empty;
    private Mood? PrimaryMood;
    private List<Mood> SecondaryMoods = new();
    private List<string> Tags = new();
    private string NewTag = string.Empty;
    private DateTime? CreatedAtUtc;
    private DateTime? UpdatedAtUtc;
    private bool HasEntry;
    private bool _isBusy;
    private string StatusMessage = string.Empty;
    private JournalEntry? _loaded;
    private bool ShowUnlockModal;
    private bool IsEntryLocked;
    private string? CurrentUnlockedEntryId;

    private bool HasPin => !string.IsNullOrEmpty(Preferences.Get("journal_pin", string.Empty));

    protected override async Task OnInitializedAsync()
    {
        await LoadEntryAsync();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        // Auto-lock when navigating away from the current unlocked entry
        if (!firstRender && _loaded != null && _loaded.Id != CurrentUnlockedEntryId && !_loaded.IsLocked)
        {
            CurrentUnlockedEntryId = null;
        }
    }

    private async Task LoadEntryAsync()
    {
        _isBusy = true;
        StatusMessage = string.Empty;
        var entry = await JournalService.GetEntryAsync(SelectedDate);

        if (entry is not null)
        {
            _loaded = entry;
            IsEntryLocked = entry.IsLocked && entry.Id != CurrentUnlockedEntryId;

            if (IsEntryLocked)
            {
                // Show locked state
                Content = string.Empty;
                PrimaryMood = null;
                SecondaryMoods = new();
                Tags = new();
                CreatedAtUtc = null;
                UpdatedAtUtc = null;
                HasEntry = true;
                ShowUnlockModal = true;
            }
            else
            {
                Content = entry.Content;
                PrimaryMood = MoodCatalog.Get(entry.PrimaryMood);
                SecondaryMoods = entry.SecondaryMoods.Select(id => MoodCatalog.Get(id)).Where(m => m is not null).Cast<Mood>().ToList();
                Tags = entry.Tags.ToList();
                CreatedAtUtc = entry.CreatedAt;
                UpdatedAtUtc = entry.UpdatedAt;
                HasEntry = true;
            }
        }
        else
        {
            _loaded = null;
            Content = string.Empty;
            PrimaryMood = null;
            SecondaryMoods = new();
            Tags = new();
            CreatedAtUtc = null;
            UpdatedAtUtc = null;
            HasEntry = false;
        }

        _isBusy = false;
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Content) || PrimaryMood is null)
        {
            StatusMessage = "Add text and select a primary mood before saving.";
            return;
        }

        _isBusy = true;
        StatusMessage = string.Empty;

        var saved = await JournalService.UpsertEntryAsync(SelectedDate, Content.Trim(), PrimaryMood.Id, SecondaryMoods.Select(m => m.Id).ToList(), Tags.ToList());
        _loaded = saved;
        Content = saved.Content;
        PrimaryMood = MoodCatalog.Get(saved.PrimaryMood);
        SecondaryMoods = saved.SecondaryMoods.Select(id => MoodCatalog.Get(id)).Where(m => m is not null).Cast<Mood>().ToList();
        Tags = saved.Tags.ToList();
        CreatedAtUtc = saved.CreatedAt;
        UpdatedAtUtc = saved.UpdatedAt;
        HasEntry = true;
        StatusMessage = "Entry saved.";
        _isBusy = false;
    }

    private async Task DeleteAsync()
    {
        _isBusy = true;
        StatusMessage = string.Empty;

        var removed = await JournalService.DeleteEntryAsync(SelectedDate);
        if (removed)
        {
            Content = string.Empty;
            PrimaryMood = null;
            SecondaryMoods = new();
            Tags = new();
            CreatedAtUtc = null;
            UpdatedAtUtc = null;
            HasEntry = false;
            StatusMessage = "Entry deleted.";
        }
        else
        {
            StatusMessage = "No entry to delete for this date.";
        }

        _isBusy = false;
    }

    private async Task ChangeDay(int deltaDays)
    {
        SelectedDate = SelectedDate.AddDays(deltaDays);
        await LoadEntryAsync();
    }

    private bool IsToday => SelectedDate == DateOnly.FromDateTime(DateTime.Now);

    private bool CanSave => !string.IsNullOrWhiteSpace(Content) && PrimaryMood is not null && HasChanges;

    private bool HasChanges
    {
        get
        {
            if (_loaded is null)
            {
                return !string.IsNullOrWhiteSpace(Content) || PrimaryMood is not null || SecondaryMoods.Any() || Tags.Any();
            }

            var contentChanged = !string.Equals(Content, _loaded.Content, StringComparison.Ordinal);
            var primaryChanged = !string.Equals(PrimaryMood?.Id, _loaded.PrimaryMood, StringComparison.Ordinal);
            var secondaryChanged = !SecondaryMoods.Select(m => m.Id).SequenceEqual(_loaded.SecondaryMoods);
            var tagsChanged = !Tags.SequenceEqual(_loaded.Tags);
            return contentChanged || primaryChanged || secondaryChanged || tagsChanged;
        }
    }

    private void AddTag()
    {
        var trimmed = (NewTag ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return;
        }

        if (!Tags.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
        {
            Tags.Add(trimmed);
        }

        NewTag = string.Empty;
    }

    private void RemoveTag(string tag)
    {
        Tags.Remove(tag);
    }

    private void OnTagKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            AddTag();
        }
    }

    private async Task ToggleLock()
    {
        if (_loaded == null) return;

        _isBusy = true;
        _loaded.IsLocked = !_loaded.IsLocked;

        if (_loaded.IsLocked)
        {
            CurrentUnlockedEntryId = null;
        }

        await JournalService.UpdateLockStatusAsync(_loaded.Id, _loaded.IsLocked);
        IsEntryLocked = _loaded.IsLocked;
        StatusMessage = _loaded.IsLocked ? "Entry locked" : "Entry unlocked";
        _isBusy = false;
    }

    private async Task HandleUnlockSubmit(string pin)
    {
        var storedPin = Preferences.Get("journal_pin", string.Empty);

        if (pin == storedPin && _loaded != null)
        {
            CurrentUnlockedEntryId = _loaded.Id;
            ShowUnlockModal = false;
            await LoadEntryAsync(); // Reload to show content
        }
        else
        {
            StatusMessage = "Incorrect PIN";
            ShowUnlockModal = false;
            await ChangeDay(0); // Reload current day
        }
    }

    private async Task HandleUnlockCancel()
    {
        ShowUnlockModal = false;
        await ChangeDay(-1); // Navigate away
    }
}
