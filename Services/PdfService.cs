using journalstart.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Colors = QuestPDF.Helpers.Colors;

namespace journalstart.Services;

public class PdfService
{
    public async Task<byte[]> GenerateJournalPdfAsync(List<JournalEntry> entries, string title = "Journal Entries", bool showLockedContent = true)
    {
        return await Task.Run(() =>
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Black));

                    // Header
                    page.Header()
                        .PaddingBottom(10)
                        .BorderBottom(2)
                        .BorderColor(Colors.Blue.Medium)
                        .Row(row =>
                        {
                            row.RelativeItem()
                                .Column(column =>
                                {
                                    column.Item()
                                        .Text(title)
                                        .FontSize(24)
                                        .Bold()
                                        .FontColor(Colors.Blue.Darken3);

                                    column.Item()
                                        .Text($"Generated: {DateTime.Now:MMMM dd, yyyy}")
                                        .FontSize(10)
                                        .FontColor(Colors.Grey.Darken1);
                                });

                            row.ConstantItem(60)
                                .AlignRight()
                                .Text("📔")
                                .FontSize(40);
                        });

                    // Content
                    page.Content()
                        .PaddingVertical(15)
                        .Column(column =>
                        {
                            if (!entries.Any())
                            {
                                column.Item()
                                    .AlignCenter()
                                    .AlignMiddle()
                                    .PaddingVertical(50)
                                    .Text("No entries to export")
                                    .FontSize(16)
                                    .Italic()
                                    .FontColor(Colors.Grey.Medium);
                                return;
                            }

                            // Summary statistics
                            column.Item()
                                .PaddingBottom(15)
                                .Background(Colors.Blue.Lighten5)
                                .Padding(12)
                                .Row(row =>
                                {
                                    row.RelativeItem()
                                        .Column(col =>
                                        {
                                            col.Item().Text("Total Entries").FontSize(10).FontColor(Colors.Grey.Darken1);
                                            col.Item().Text(entries.Count.ToString()).FontSize(20).Bold().FontColor(Colors.Blue.Darken2);
                                        });

                                    row.RelativeItem()
                                        .Column(col =>
                                        {
                                            col.Item().Text("Date Range").FontSize(10).FontColor(Colors.Grey.Darken1);
                                            var dateRange = entries.Count > 0
                                                ? $"{entries.Min(e => e.DateOnly):MMM dd} - {entries.Max(e => e.DateOnly):MMM dd, yyyy}"
                                                : "N/A";
                                            col.Item().Text(dateRange).FontSize(12).Bold().FontColor(Colors.Blue.Darken2);
                                        });

                                    row.RelativeItem()
                                        .Column(col =>
                                        {
                                            col.Item().Text("Total Words").FontSize(10).FontColor(Colors.Grey.Darken1);
                                            var totalWords = entries.Sum(e => GetWordCount(StripHtml(e.Content)));
                                            col.Item().Text(totalWords.ToString("N0")).FontSize(20).Bold().FontColor(Colors.Blue.Darken2);
                                        });
                                });

                            column.Item().PaddingTop(15);

                            // Journal entries
                            foreach (var entry in entries.OrderByDescending(e => e.DateOnly))
                            {
                                column.Item()
                                    .PaddingBottom(15)
                                    .Column(entryColumn =>
                                    {
                                        // Entry header
                                        entryColumn.Item()
                                            .Background(Colors.Grey.Lighten3)
                                            .Padding(10)
                                            .Row(headerRow =>
                                            {
                                                headerRow.RelativeItem()
                                                    .Column(col =>
                                                    {
                                                        col.Item()
                                                            .Text(entry.DateOnly.ToString("dddd, MMMM dd, yyyy"))
                                                            .FontSize(14)
                                                            .Bold()
                                                            .FontColor(Colors.Blue.Darken3);

                                                        // Mood display
                                                        if (!string.IsNullOrEmpty(entry.PrimaryMood))
                                                        {
                                                            var mood = MoodCatalog.Get(entry.PrimaryMood);
                                                            if (mood != null)
                                                            {
                                                                var moodText = $"{mood.Emoji} {mood.Name}";
                                                                if (entry.SecondaryMoods.Any())
                                                                {
                                                                    var secondaryEmojis = string.Join(" ",
                                                                        entry.SecondaryMoods
                                                                            .Select(id => MoodCatalog.Get(id)?.Emoji ?? "")
                                                                            .Where(e => !string.IsNullOrEmpty(e)));
                                                                    moodText += $" +{secondaryEmojis}";
                                                                }
                                                                col.Item()
                                                                    .Text(moodText)
                                                                    .FontSize(11)
                                                                    .FontColor(Colors.Grey.Darken1);
                                                            }
                                                        }
                                                    });

                                                headerRow.ConstantItem(80)
                                                    .AlignRight()
                                                    .Column(col =>
                                                    {
                                                        col.Item()
                                                            .Text($"{GetWordCount(StripHtml(entry.Content))} words")
                                                            .FontSize(9)
                                                            .FontColor(Colors.Grey.Darken1);

                                                        if (entry.IsLocked)
                                                        {
                                                            col.Item()
                                                                .Text("🔒 Locked")
                                                                .FontSize(9)
                                                                .FontColor(Colors.Orange.Darken1);
                                                        }
                                                    });
                                            });

                                        // Entry content
                                        entryColumn.Item()
                                            .Border(1)
                                            .BorderColor(Colors.Grey.Lighten2)
                                            .Padding(12)
                                            .Column(contentColumn =>
                                            {
                                                // Show content if entry is unlocked OR if showLockedContent is true (PIN verified)
                                                if (entry.IsLocked && !showLockedContent)
                                                {
                                                    contentColumn.Item()
                                                        .Text("🔒 This entry is locked and protected.")
                                                        .Italic()
                                                        .FontColor(Colors.Grey.Medium);
                                                }
                                                else
                                                {
                                                    var text = StripHtml(entry.Content);
                                                    if (!string.IsNullOrWhiteSpace(text))
                                                    {
                                                        contentColumn.Item()
                                                            .Text(text)
                                                            .FontSize(10)
                                                            .LineHeight(1.5f);
                                                    }
                                                }

                                                // Tags - show for all entries when showLockedContent is true
                                                if (entry.Tags.Any() && (!entry.IsLocked || showLockedContent))
                                                {
                                                    contentColumn.Item()
                                                        .PaddingTop(10)
                                                        .Row(tagRow =>
                                                        {
                                                            tagRow.AutoItem()
                                                                .Text("Tags: ")
                                                                .FontSize(9)
                                                                .FontColor(Colors.Grey.Darken1);

                                                            tagRow.RelativeItem()
                                                                .Text(string.Join(", ", entry.Tags))
                                                                .FontSize(9)
                                                                .FontColor(Colors.Blue.Medium);
                                                        });
                                                }
                                            });
                                    });
                            }
                        });

                    // Footer
                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                });
            });

            return document.GeneratePdf();
        });
    }

    public async Task SaveAndOpenPdfAsync(byte[] pdfBytes, string filename)
    {
        try
        {
            // Save to temp location
            var tempPath = Path.Combine(FileSystem.AppDataDirectory, "temp");
            Directory.CreateDirectory(tempPath);

            var filePath = Path.Combine(tempPath, filename);
            await File.WriteAllBytesAsync(filePath, pdfBytes);

            // Open with default PDF viewer
            await Launcher.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(filePath)
            });
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to save and open PDF: {ex.Message}", ex);
        }
    }

    public async Task<string> SavePdfToDownloadsAsync(byte[] pdfBytes, string filename)
    {
        try
        {
            // Use FileSaver from CommunityToolkit.Maui
            using var stream = new MemoryStream(pdfBytes);
            var fileSaverResult = await CommunityToolkit.Maui.Storage.FileSaver.Default.SaveAsync(filename, stream);

            if (fileSaverResult.IsSuccessful)
            {
                return fileSaverResult.FilePath;
            }
            else
            {
                throw new Exception($"Failed to save file: {fileSaverResult.Exception?.Message}");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to save PDF: {ex.Message}", ex);
        }
    }

    private string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html))
            return string.Empty;

        return Regex.Replace(html, "<[^>]+>", " ").Trim();
    }

    private int GetWordCount(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
