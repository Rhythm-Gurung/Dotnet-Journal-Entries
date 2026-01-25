using System.Text.RegularExpressions;
using journalstart.Models;
using journalstart.Services;
using Microsoft.AspNetCore.Components;

namespace journalstart.Components.Pages;

public partial class Analytics
{
    [Inject] private JournalService JournalService { get; set; } = default!;

    private string SelectedRange { get; set; } = "30d";
    private AnalyticsStats Stats { get; set; } = new();

    private Dictionary<string, string> DateRanges = new()
    {
        { "7d", "7 Days" },
        { "30d", "30 Days" },
        { "90d", "90 Days" },
        { "all", "All Time" }
    };

    protected override async Task OnInitializedAsync()
    {
        await CalculateStats();
    }

    private async Task SetDateRange(string range)
    {
        SelectedRange = range;
        await CalculateStats();
    }

    private async Task CalculateStats()
    {
        var allEntries = await JournalService.GetAllEntriesAsync();
        var filteredEntries = FilterEntriesByDateRange(allEntries);

        Stats = new AnalyticsStats
        {
            TotalEntries = filteredEntries.Count,
            AverageWordCount = filteredEntries.Any() ? (int)filteredEntries.Average(e => GetWordCount(e.Content)) : 0,
            CurrentStreak = CalculateCurrentStreak(allEntries),
            LongestStreak = CalculateLongestStreak(allEntries),
            MoodDistribution = CalculateMoodDistribution(filteredEntries),
            MostFrequentMood = GetMostFrequentMood(filteredEntries),
            TopTags = GetTopTags(filteredEntries),
            MissedDays = CalculateMissedDays(allEntries)
        };

        Stats.TotalMoodEntries = Stats.MoodDistribution.Positive + Stats.MoodDistribution.Neutral + Stats.MoodDistribution.Negative;
    }

    private List<JournalEntry> FilterEntriesByDateRange(List<JournalEntry> entries)
    {
        if (SelectedRange == "all")
            return entries;

        var days = SelectedRange switch
        {
            "7d" => 7,
            "30d" => 30,
            "90d" => 90,
            _ => 30
        };

        var cutoffDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-days));
        return entries.Where(e => e.DateOnly >= cutoffDate).ToList();
    }

    private int CalculateCurrentStreak(List<JournalEntry> entries)
    {
        if (!entries.Any()) return 0;

        var sortedDates = entries.Select(e => e.DateOnly).Distinct().OrderDescending().ToList();
        var today = DateOnly.FromDateTime(DateTime.Now);

        if (!sortedDates.Contains(today) && !sortedDates.Contains(today.AddDays(-1)))
            return 0;

        int streak = 0;
        var currentDate = sortedDates.Contains(today) ? today : today.AddDays(-1);

        foreach (var date in sortedDates)
        {
            if (date == currentDate)
            {
                streak++;
                currentDate = currentDate.AddDays(-1);
            }
            else if (date < currentDate)
            {
                break;
            }
        }

        return streak;
    }

    private int CalculateLongestStreak(List<JournalEntry> entries)
    {
        if (!entries.Any()) return 0;

        var sortedDates = entries.Select(e => e.DateOnly).Distinct().OrderBy(d => d).ToList();
        int longestStreak = 1;
        int currentStreak = 1;

        for (int i = 1; i < sortedDates.Count; i++)
        {
            if (sortedDates[i].DayNumber - sortedDates[i - 1].DayNumber == 1)
            {
                currentStreak++;
                longestStreak = Math.Max(longestStreak, currentStreak);
            }
            else
            {
                currentStreak = 1;
            }
        }

        return longestStreak;
    }

    private MoodDistribution CalculateMoodDistribution(List<JournalEntry> entries)
    {
        var distribution = new MoodDistribution();

        foreach (var entry in entries.Where(e => !string.IsNullOrEmpty(e.PrimaryMood)))
        {
            var mood = MoodCatalog.Get(entry.PrimaryMood);
            if (mood != null)
            {
                switch (mood.Category)
                {
                    case MoodCategory.Positive:
                        distribution.Positive++;
                        break;
                    case MoodCategory.Neutral:
                        distribution.Neutral++;
                        break;
                    case MoodCategory.Negative:
                        distribution.Negative++;
                        break;
                }
            }
        }

        return distribution;
    }

    private Mood? GetMostFrequentMood(List<JournalEntry> entries)
    {
        var moodCounts = entries
            .Where(e => !string.IsNullOrEmpty(e.PrimaryMood))
            .GroupBy(e => e.PrimaryMood)
            .Select(g => new { MoodId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .FirstOrDefault();

        return moodCounts != null ? MoodCatalog.Get(moodCounts.MoodId) : null;
    }

    private List<TagCount> GetTopTags(List<JournalEntry> entries)
    {
        return entries
            .SelectMany(e => e.Tags)
            .GroupBy(t => t)
            .Select(g => new TagCount { Tag = g.Key, Count = g.Count() })
            .OrderByDescending(t => t.Count)
            .Take(8)
            .ToList();
    }

    private List<DateOnly> CalculateMissedDays(List<JournalEntry> entries)
    {
        if (SelectedRange == "all") return new List<DateOnly>();

        var days = SelectedRange switch
        {
            "7d" => 7,
            "30d" => 30,
            "90d" => 90,
            _ => 30
        };

        var startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-days));
        var endDate = DateOnly.FromDateTime(DateTime.Now);
        var entryDates = entries.Select(e => e.DateOnly).ToHashSet();
        var missedDays = new List<DateOnly>();

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (!entryDates.Contains(date))
            {
                missedDays.Add(date);
            }
        }

        return missedDays;
    }

    private int GetWordCount(string html)
    {
        var text = Regex.Replace(html ?? string.Empty, "<[^>]+>", " ");
        return text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private string GetRangeLabel()
    {
        return SelectedRange == "all" ? "All time" : $"Last {SelectedRange.Replace("d", " days")}";
    }

    private int GetPercentage(int value)
    {
        return Stats.TotalMoodEntries > 0 ? (int)Math.Round(value * 100.0 / Stats.TotalMoodEntries) : 0;
    }

    private int GetTagPercentage(int count)
    {
        var maxCount = Stats.TopTags.Any() ? Stats.TopTags.Max(t => t.Count) : 1;
        return (int)Math.Round(count * 100.0 / maxCount);
    }

    private class AnalyticsStats
    {
        public int TotalEntries { get; set; }
        public int AverageWordCount { get; set; }
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public MoodDistribution MoodDistribution { get; set; } = new();
        public int TotalMoodEntries { get; set; }
        public Mood? MostFrequentMood { get; set; }
        public List<TagCount> TopTags { get; set; } = new();
        public List<DateOnly> MissedDays { get; set; } = new();
    }

    private class MoodDistribution
    {
        public int Positive { get; set; }
        public int Neutral { get; set; }
        public int Negative { get; set; }
    }

    private class TagCount
    {
        public string Tag { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
