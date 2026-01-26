using System.Text.RegularExpressions;
using journalstart.Models;
using journalstart.Services;
using Microsoft.AspNetCore.Components;

namespace journalstart.Components.Pages;

public partial class Insights
{
    [Inject] private JournalService JournalService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private List<JournalEntry> AllEntries { get; set; } = new();
    private List<JournalEntry> FilteredEntries { get; set; } = new();
    private List<string> AllTags { get; set; } = new();

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
        SelectedEntryId = entry.Id;
        Navigation.NavigateTo($"/today?date={entry.DateOnly:yyyy-MM-dd}");
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
