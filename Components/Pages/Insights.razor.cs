using System.Text.RegularExpressions;
using journalstart.Models;
using journalstart.Services;
using Microsoft.AspNetCore.Components;

namespace journalstart.Components.Pages;

public partial class Insights
{
    [Inject] private JournalService JournalService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private PdfService PdfService { get; set; } = default!;

    private List<JournalEntry> AllEntries { get; set; } = new();
    private List<JournalEntry> FilteredEntries { get; set; } = new();
    private List<string> AllTags { get; set; } = new();

    // Selection mode for PDF export
    private bool IsSelectionMode { get; set; } = false;
    private HashSet<string> SelectedEntryIds { get; set; } = new();
    private bool IsExporting { get; set; } = false;
    private string ExportMessage { get; set; } = string.Empty;

    // PIN verification for locked entries
    private bool ShowPinModalForExport { get; set; } = false;
    private string ExportPin { get; set; } = string.Empty;
    private string PinErrorMessage { get; set; } = string.Empty;
    private const string PIN_KEY = "journal_pin_lock";

    private bool ShowFilters { get; set; }

    private string? _startDate;
    private string? StartDate
    {
        get => _startDate;
        set
        {
            _startDate = value;
            CurrentPage = 1;
            ApplyFilters();
        }
    }

    private string? _endDate;
    private string? EndDate
    {
        get => _endDate;
        set
        {
            _endDate = value;
            CurrentPage = 1;
            ApplyFilters();
        }
    }

    private HashSet<string> SelectedMoods { get; set; } = new();
    private HashSet<string> SelectedTags { get; set; } = new();

    private string? SelectedEntryId { get; set; }
    private int CurrentPage { get; set; } = 1;
    private int PageSize { get; set; } = 10;

    protected override async Task OnInitializedAsync()
    {
        await LoadEntries();
        ExtractAllTags();
        ApplyFilters();
    }

    private async Task LoadEntries()
    {
        AllEntries = await JournalService.GetAllEntriesAsync();
    }

    private void ExtractAllTags()
    {
        AllTags = AllEntries
            .SelectMany(e => e.Tags)
            .Distinct()
            .OrderBy(t => t)
            .ToList();
    }

    private string _searchQuery = string.Empty;
    private string SearchQuery
    {
        get => _searchQuery;
        set
        {
            _searchQuery = value;
            CurrentPage = 1;
            ApplyFilters();
        }
    }

    private void ApplyFilters()
    {
        FilteredEntries = AllEntries.Where(entry =>
        {
            // Search filter
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var searchLower = SearchQuery.ToLower();
                var content = StripHtml(entry.Content).ToLower();
                if (!content.Contains(searchLower))
                    return false;
            }

            // Date range filter
            if (!string.IsNullOrEmpty(StartDate) && DateOnly.TryParse(StartDate, out var start))
            {
                if (entry.DateOnly < start)
                    return false;
            }
            if (!string.IsNullOrEmpty(EndDate) && DateOnly.TryParse(EndDate, out var end))
            {
                if (entry.DateOnly > end)
                    return false;
            }

            // Mood filter
            if (SelectedMoods.Any())
            {
                var entryMoods = new[] { entry.PrimaryMood }.Concat(entry.SecondaryMoods).Where(m => m != null);
                if (!SelectedMoods.Any(sm => entryMoods.Contains(sm)))
                    return false;
            }

            // Tag filter
            if (SelectedTags.Any())
            {
                if (!SelectedTags.Any(st => entry.Tags.Contains(st)))
                    return false;
            }

            return true;
        }).ToList();
    }

    private void ToggleFilters()
    {
        ShowFilters = !ShowFilters;
    }

    private void ToggleMood(string moodId)
    {
        if (SelectedMoods.Contains(moodId))
            SelectedMoods.Remove(moodId);
        else
            SelectedMoods.Add(moodId);

        CurrentPage = 1;
        ApplyFilters();
    }

    private void ToggleTag(string tag)
    {
        if (SelectedTags.Contains(tag))
            SelectedTags.Remove(tag);
        else
            SelectedTags.Add(tag);

        CurrentPage = 1;
        ApplyFilters();
    }

    private void ClearAllFilters()
    {
        StartDate = null;
        EndDate = null;
        SelectedMoods.Clear();
        SelectedTags.Clear();
        CurrentPage = 1;
        ApplyFilters();
    }

    private void SelectEntry(JournalEntry entry)
    {
        if (IsSelectionMode)
        {
            // Toggle selection in selection mode
            if (SelectedEntryIds.Contains(entry.Id))
                SelectedEntryIds.Remove(entry.Id);
            else
                SelectedEntryIds.Add(entry.Id);
        }
        else
        {
            // Navigate to entry in normal mode
            SelectedEntryId = entry.Id;
            Navigation.NavigateTo($"/today?date={entry.DateOnly:yyyy-MM-dd}");
        }
    }

    private void ToggleSelectionMode()
    {
        IsSelectionMode = !IsSelectionMode;
        if (!IsSelectionMode)
        {
            SelectedEntryIds.Clear();
        }
    }

    private void SelectAllEntries()
    {
        SelectedEntryIds.Clear();
        foreach (var entry in FilteredEntries)
        {
            SelectedEntryIds.Add(entry.Id);
        }
    }

    private void DeselectAllEntries()
    {
        SelectedEntryIds.Clear();
    }

    private async Task ExportSelectedToPdf()
    {
        if (!SelectedEntryIds.Any())
        {
            ExportMessage = "Please select at least one entry to export";
            return;
        }

        // Get selected entries
        var selectedEntries = AllEntries
            .Where(e => SelectedEntryIds.Contains(e.Id))
            .OrderByDescending(e => e.DateOnly)
            .ToList();

        // Check if any selected entries are locked
        var hasLockedEntries = selectedEntries.Any(e => e.IsLocked);

        if (hasLockedEntries)
        {
            // Show PIN modal for locked entries
            ShowPinModalForExport = true;
            PinErrorMessage = string.Empty;
            ExportPin = string.Empty;
            return;
        }

        // Export directly if no locked entries
        await PerformExport(selectedEntries);
    }

    private async Task VerifyPinAndExport()
    {
        PinErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(ExportPin))
        {
            PinErrorMessage = "Please enter your PIN";
            return;
        }

        if (ExportPin.Length != 4)
        {
            PinErrorMessage = "PIN must be 4 digits";
            return;
        }

        try
        {
            var storedPin = await SecureStorage.GetAsync(PIN_KEY);

            if (storedPin == ExportPin)
            {
                // PIN is correct, proceed with export
                ShowPinModalForExport = false;
                ExportPin = string.Empty;
                PinErrorMessage = string.Empty;

                var selectedEntries = AllEntries
                    .Where(e => SelectedEntryIds.Contains(e.Id))
                    .OrderByDescending(e => e.DateOnly)
                    .ToList();

                await PerformExport(selectedEntries);
            }
            else
            {
                PinErrorMessage = "Incorrect PIN. Please try again.";
                ExportPin = string.Empty;
            }
        }
        catch (Exception ex)
        {
            PinErrorMessage = $"Error verifying PIN: {ex.Message}";
        }
    }

    private void CancelPinForExport()
    {
        ShowPinModalForExport = false;
        ExportPin = string.Empty;
        PinErrorMessage = string.Empty;
    }

    private async Task PerformExport(List<JournalEntry> selectedEntries)
    {
        IsExporting = true;
        ExportMessage = string.Empty;

        try
        {
            // Generate PDF - showLockedContent = true since PIN was verified if needed
            var pdfBytes = await PdfService.GenerateJournalPdfAsync(
                selectedEntries,
                $"Journal Export - {DateTime.Now:yyyy-MM-dd}",
                showLockedContent: true
            );

            // Save PDF
            var filename = $"JournalExport_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            var savedPath = await PdfService.SavePdfToDownloadsAsync(pdfBytes, filename);

            ExportMessage = $"✓ Successfully exported {selectedEntries.Count} entries!";

            // Exit selection mode
            IsSelectionMode = false;
            SelectedEntryIds.Clear();
        }
        catch (Exception ex)
        {
            ExportMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsExporting = false;
            StateHasChanged();
        }
    }

    private string StripHtml(string html)
    {
        return Regex.Replace(html ?? string.Empty, "<[^>]+>", " ").Trim();
    }

    private int GetWordCount(string html)
    {
        var text = StripHtml(html);
        return text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private string GetMoodFilterClass(MoodCategory category, bool isSelected)
    {
        if (isSelected)
        {
            return category switch
            {
                MoodCategory.Positive => "bg-green-100 text-green-700 ring-2 ring-green-400 dark:bg-green-900/30 dark:text-green-400 dark:ring-green-600",
                MoodCategory.Neutral => "bg-blue-100 text-blue-700 ring-2 ring-blue-400 dark:bg-blue-900/30 dark:text-blue-400 dark:ring-blue-600",
                MoodCategory.Negative => "bg-red-100 text-red-700 ring-2 ring-red-400 dark:bg-red-900/30 dark:text-red-400 dark:ring-red-600",
                _ => "bg-slate-100 text-slate-700 dark:bg-slate-700 dark:text-slate-300"
            };
        }
        return "bg-slate-100 text-slate-600 hover:bg-slate-200 dark:bg-slate-700 dark:text-slate-300 dark:hover:bg-slate-600";
    }

    private int ActiveFilterCount =>
        (string.IsNullOrEmpty(StartDate) ? 0 : 1) +
        (string.IsNullOrEmpty(EndDate) ? 0 : 1) +
        SelectedMoods.Count +
        SelectedTags.Count;

    // Pagination
    private List<JournalEntry> PaginatedEntries =>
        FilteredEntries
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

    private int TotalPages => (int)Math.Ceiling(FilteredEntries.Count / (double)PageSize);

    private void PreviousPage()
    {
        if (CurrentPage > 1)
            CurrentPage--;
    }

    private void NextPage()
    {
        if (CurrentPage < TotalPages)
            CurrentPage++;
    }

    private void GoToPage(int page)
    {
        CurrentPage = page;
    }
}
