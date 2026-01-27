using journalstart.Models;
using journalstart.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace journalstart.Components.Pages;

public partial class Today
{
    [Inject] private JournalService JournalService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "date")]
    public string? DateQuery { get; set; }

    private DateOnly SelectedDate = DateOnly.FromDateTime(DateTime.Now);
    private string Content = string.Empty;
    private Mood? PrimaryMood;
    private List<Mood> SecondaryMoods = new();
    private List<string> Tags = new();
    private string NewTag = string.Empty;
    private readonly string[] PredefinedTags = new[]
    {
        "Work","Personal","Health","Fitness","Family","Finance","Study","Travel","Gratitude","Idea","Project","Meeting","Sleep","Food","Hobby"
    };
    private DateTime? CreatedAtUtc;
    private DateTime? UpdatedAtUtc;
    private bool HasEntry;
    private bool _isBusy;
    private string StatusMessage = string.Empty;
    private JournalEntry? _loaded;
    private bool IsLocked = false;
    private bool IsPinSet = false;
    private bool IsViewLocked = false;
    private bool ShowPinModal = false;
    private string EnteredPin = string.Empty;
    private string PinErrorMessage = string.Empty;

    private const string PIN_KEY = "journal_pin_lock";

    protected override async Task OnInitializedAsync()
    {
        // Parse date from query parameter if provided
        if (!string.IsNullOrEmpty(DateQuery) && DateOnly.TryParse(DateQuery, out var parsedDate))
        {
            SelectedDate = parsedDate;
        }

        await CheckPinStatus();
        await LoadEntryAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        // Handle date changes from navigation
        if (!string.IsNullOrEmpty(DateQuery) && DateOnly.TryParse(DateQuery, out var parsedDate))
        {
            if (SelectedDate != parsedDate)
            {
                SelectedDate = parsedDate;
                await LoadEntryAsync();
            }
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
            IsLocked = entry.IsLocked;

            // If entry is locked, show PIN modal and lock view
            if (entry.IsLocked)
            {
                IsViewLocked = true;
                ShowPinModal = true;
                Content = string.Empty;
                PrimaryMood = null;
                SecondaryMoods = new();
                Tags = new();
            }
            else
            {
                IsViewLocked = false;
                ShowPinModal = false;
                Content = entry.Content;
                PrimaryMood = MoodCatalog.Get(entry.PrimaryMood);
                SecondaryMoods = entry.SecondaryMoods.Select(id => MoodCatalog.Get(id)).Where(m => m is not null).Cast<Mood>().ToList();
                Tags = entry.Tags.ToList();
            }

            CreatedAtUtc = entry.CreatedAt;
            UpdatedAtUtc = entry.UpdatedAt;
            HasEntry = true;
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
            IsLocked = false;
            IsViewLocked = false;
            ShowPinModal = false;
            HasEntry = false;
        }

        _isBusy = false;
    }

    private async Task SaveAsync()
    {
        // Check if creating a new entry for a past date
        if (!HasEntry && !CanCreateEntry)
        {
            if (IsPastDate)
                StatusMessage = "You cannot create new entries for past dates. Only today's entries can be created.";
            else if (IsFutureDate)
                StatusMessage = "You cannot create entries for future dates.";
            return;
        }

        // Check if editing an entry for a non-editable date
        if (!CanEditDate)
        {
            StatusMessage = "You can only edit entries for today or existing past entries.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Content) || PrimaryMood is null)
        {
            StatusMessage = "Add text and select a primary mood before saving.";
            return;
        }

        _isBusy = true;
        StatusMessage = string.Empty;

        // Save with current lock state - each entry maintains its own independent lock status
        var saved = await JournalService.UpsertEntryAsync(SelectedDate, Content.Trim(), PrimaryMood.Id, SecondaryMoods.Select(m => m.Id).ToList(), Tags.ToList(), IsLocked);
        _loaded = saved;
        Content = saved.Content;
        PrimaryMood = MoodCatalog.Get(saved.PrimaryMood);
        SecondaryMoods = saved.SecondaryMoods.Select(id => MoodCatalog.Get(id)).Where(m => m is not null).Cast<Mood>().ToList();
        Tags = saved.Tags.ToList();
        IsLocked = saved.IsLocked;
        CreatedAtUtc = saved.CreatedAt;
        UpdatedAtUtc = saved.UpdatedAt;
        HasEntry = true;
        StatusMessage = IsLocked ? "Entry saved and locked." : "Entry saved.";
        _isBusy = false;
    }

    private async Task DeleteAsync()
    {
        // Check if user is trying to delete a non-today date entry
        if (!CanEditDate)
        {
            if (IsPastDate)
                StatusMessage = "You can only delete today's entry. Past entries cannot be deleted.";
            else if (IsFutureDate)
                StatusMessage = "Future entries cannot be deleted.";
            return;
        }

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
            IsLocked = false;
            IsViewLocked = false;
            ShowPinModal = false;
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

    private bool IsPastDate => SelectedDate < DateOnly.FromDateTime(DateTime.Now);

    private bool IsFutureDate => SelectedDate > DateOnly.FromDateTime(DateTime.Now);

    // Allow editing if: (1) It's today, OR (2) It's a past date with an existing entry
    // Prevent: Creating new entries for past dates, or any future date operations
    private bool CanEditDate => IsToday || (IsPastDate && HasEntry);

    // Prevent creating new entries for past dates
    private bool CanCreateEntry => IsToday;

    private bool CanSave => !string.IsNullOrWhiteSpace(Content) && PrimaryMood is not null && HasChanges && CanEditDate;

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
            var lockChanged = IsLocked != _loaded.IsLocked;
            return contentChanged || primaryChanged || secondaryChanged || tagsChanged || lockChanged;
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

    private void AddPredefinedTag(string tag)
    {
        var trimmed = (tag ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return;
        }

        if (!Tags.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
        {
            Tags.Add(trimmed);
        }
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

    private string GetPresetTagClass(string tag)
    {
        var isSelected = Tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
        if (isSelected)
        {
            return "rounded bg-slate-200 px-2 py-1 text-xs text-slate-700 cursor-default dark:bg-slate-700 dark:text-slate-300";
        }
        return "rounded bg-indigo-50 px-2 py-1 text-xs text-indigo-700 hover:bg-indigo-100 dark:bg-indigo-900/20 dark:text-indigo-300";
    }

    private async Task CheckPinStatus()
    {
        try
        {
            var storedPin = await SecureStorage.GetAsync(PIN_KEY);
            IsPinSet = !string.IsNullOrEmpty(storedPin);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking PIN status: {ex.Message}");
            IsPinSet = false;
        }
    }

    private void ToggleLock()
    {
        if (!IsPinSet)
        {
            StatusMessage = "Please set a PIN in Settings first.";
            return;
        }

        IsLocked = !IsLocked;
        StatusMessage = IsLocked ? "Entry will be locked when saved." : "Lock removed from entry.";
    }

    private string GetLockButtonClass()
    {
        if (IsLocked)
        {
            return "flex items-center gap-2 rounded-lg bg-amber-100 px-3 py-2 text-sm font-medium text-amber-800 transition hover:bg-amber-200 dark:bg-amber-900/30 dark:text-amber-300 dark:hover:bg-amber-900/50";
        }
        return "flex items-center gap-2 rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-600 transition hover:bg-slate-50 dark:border-slate-600 dark:bg-slate-700 dark:text-slate-300 dark:hover:bg-slate-600";
    }

    private async Task VerifyPin()
    {
        PinErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(EnteredPin))
        {
            PinErrorMessage = "Please enter your PIN";
            return;
        }

        if (EnteredPin.Length != 4)
        {
            PinErrorMessage = "PIN must be 4 digits";
            return;
        }

        try
        {
            var storedPin = await SecureStorage.GetAsync(PIN_KEY);

            if (storedPin == EnteredPin)
            {
                // PIN is correct, unlock the view (but entry remains locked)
                IsViewLocked = false;
                ShowPinModal = false;
                EnteredPin = string.Empty;
                PinErrorMessage = string.Empty;

                // Load the actual content (IsLocked stays true)
                if (_loaded != null)
                {
                    Content = _loaded.Content;
                    PrimaryMood = MoodCatalog.Get(_loaded.PrimaryMood);
                    SecondaryMoods = _loaded.SecondaryMoods.Select(id => MoodCatalog.Get(id)).Where(m => m is not null).Cast<Mood>().ToList();
                    Tags = _loaded.Tags.ToList();
                    // IsLocked remains true - this is the entry's saved lock status
                }

                StateHasChanged();
            }
            else
            {
                PinErrorMessage = "Incorrect PIN. Please try again.";
                EnteredPin = string.Empty;
            }
        }
        catch (Exception ex)
        {
            PinErrorMessage = $"Error verifying PIN: {ex.Message}";
        }
    }

    private void CancelPinEntry()
    {
        ShowPinModal = false;
        EnteredPin = string.Empty;
        PinErrorMessage = string.Empty;

        // User canceled, navigate to previous day
        _ = ChangeDay(-1);
    }

    private void OnPinKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            _ = VerifyPin();
        }
    }
}
