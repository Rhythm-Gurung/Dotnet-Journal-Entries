using journalstart.Models;
using journalstart.Services;
using Microsoft.AspNetCore.Components;

namespace journalstart.Components.Pages;

public partial class Timeline
{
    [Inject] private JournalService JournalService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private DateTime CurrentMonth { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
    private DateOnly SelectedDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    private JournalEntry? SelectedEntry { get; set; }
    private Dictionary<DateOnly, JournalEntry> EntryMap { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadEntries();
        await LoadSelectedEntry();
    }

    private async Task LoadEntries()
    {
        // Get all entries from the service
        var allEntries = await JournalService.GetAllEntriesAsync();

        EntryMap.Clear();
        foreach (var entry in allEntries)
        {
            EntryMap[entry.DateOnly] = entry;
        }
    }

    private async Task LoadSelectedEntry()
    {
        SelectedEntry = await JournalService.GetEntryAsync(SelectedDate);
    }

    private async Task SelectDate(DateOnly date)
    {
        SelectedDate = date;
        await LoadSelectedEntry();
    }

    private bool IsToday(DateOnly date) => date == DateOnly.FromDateTime(DateTime.Now);

    private bool IsPastDate => SelectedDate < DateOnly.FromDateTime(DateTime.Now);

    // Allow editing if: (1) It's today, OR (2) It's a past date with an existing entry
    private bool CanEditDate => IsToday(SelectedDate) || (IsPastDate && SelectedEntry != null);

    // Only allow creating new entries for today
    private bool CanCreateEntry => IsToday(SelectedDate);

    private async Task PreviousMonth()
    {
        CurrentMonth = CurrentMonth.AddMonths(-1);
        await LoadEntries();
    }

    private async Task NextMonth()
    {
        CurrentMonth = CurrentMonth.AddMonths(1);
        await LoadEntries();
    }

    private void NavigateToEdit()
    {
        Navigation.NavigateTo($"/today?date={SelectedDate:yyyy-MM-dd}");
    }

    private List<CalendarDay> CalendarDays
    {
        get
        {
            var days = new List<CalendarDay>();
            var firstDay = CurrentMonth;
            var lastDay = firstDay.AddMonths(1).AddDays(-1);

            // Get the first day to display (may be from previous month)
            var startDay = firstDay.AddDays(-(int)firstDay.DayOfWeek);

            // Get the last day to display (may be from next month)
            var endDay = lastDay.AddDays(6 - (int)lastDay.DayOfWeek);

            for (var date = DateOnly.FromDateTime(startDay); date <= DateOnly.FromDateTime(endDay); date = date.AddDays(1))
            {
                var hasEntry = EntryMap.ContainsKey(date);
                var isLocked = hasEntry && EntryMap[date].IsLocked;

                days.Add(new CalendarDay
                {
                    Date = date,
                    IsCurrentMonth = date.Month == CurrentMonth.Month,
                    HasEntry = hasEntry,
                    IsLocked = isLocked
                });
            }

            return days;
        }
    }

    private string GetDayClasses(CalendarDay day)
    {
        var classes = new List<string>();

        if (!day.IsCurrentMonth)
        {
            classes.Add("text-slate-300 dark:text-slate-600");
        }
        else if (day.Date == DateOnly.FromDateTime(DateTime.Now))
        {
            classes.Add("bg-slate-200 font-semibold dark:bg-slate-700");
        }
        else
        {
            classes.Add("hover:bg-slate-100 dark:hover:bg-slate-700");
        }

        if (day.Date == SelectedDate)
        {
            classes.Add("ring-2 ring-slate-800 font-semibold dark:ring-slate-200");
        }

        return string.Join(" ", classes);
    }

    private class CalendarDay
    {
        public DateOnly Date { get; set; }
        public bool IsCurrentMonth { get; set; }
        public bool HasEntry { get; set; }
        public bool IsLocked { get; set; }
    }
}
