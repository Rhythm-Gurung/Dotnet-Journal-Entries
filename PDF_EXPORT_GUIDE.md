# PDF Export Feature - User Guide

## Overview

The journal app now includes a comprehensive PDF export feature that allows you to select specific journal entries and export them to a beautifully formatted PDF document.

## How to Use

### 1. Navigate to Insights Page

- Open the journal app and go to the **Insights** page from the navigation menu

### 2. Enter Selection Mode

- Click the **"Export PDF"** button (green button with download icon) in the top toolbar
- This will activate **Selection Mode**

### 3. Select Entries to Export

You have several options:

- **Click individual entries** to select/deselect them (checkboxes will appear)
- **Select All** button - selects all entries currently visible (respects filters)
- **Clear** button - deselects all entries

### 4. Export to PDF

- Once you've selected the entries you want, click the **"Export PDF"** button in the selection toolbar
- The system will generate a PDF and prompt you to save it to your preferred location
- The PDF will automatically open in your default PDF viewer

### 5. Exit Selection Mode

- Click **"Cancel"** to exit selection mode without exporting

## PDF Features

### Document Layout

- **A4 page size** with professional margins
- **Header** with title and generation date
- **Summary statistics** showing:
  - Total number of entries
  - Date range covered
  - Total word count across all entries
- **Page numbers** in footer

### Entry Display

Each journal entry includes:

- **Date** in full format (e.g., "Monday, January 26, 2026")
- **Mood indicators** with emoji and names
- **Entry content** (locked entries show as protected)
- **Word count** for each entry
- **Lock status** indicator for protected entries
- **Tags** listed at the bottom of each entry

### Privacy Protection

- **Locked entries** are exported with a privacy message instead of content
- Lock status is clearly indicated with a 🔒 icon
- Content remains protected even in exported PDFs

## Tips

### Using Filters Before Export

1. Use the search and filter features to narrow down entries
2. Enter selection mode
3. Use "Select All" to select all filtered entries
4. Export only the entries matching your criteria

### Common Export Scenarios

- **Monthly review**: Filter by date range → Select All → Export
- **Mood analysis**: Filter by specific moods → Select All → Export
- **Tag-based export**: Filter by tags → Select All → Export
- **Custom selection**: Manually select specific entries across different dates

## Technical Details

### Packages Used

- **QuestPDF** (2025.12.3) - PDF generation library
- **CommunityToolkit.Maui** (9.1.1) - Cross-platform file saving

### File Naming

Exported PDFs are automatically named with the format:

```
JournalExport_YYYYMMDD_HHMMSS.pdf
```

Example: `JournalExport_20260126_143052.pdf`

### Permissions

On Android, the app requires storage permissions to save PDF files:

- `WRITE_EXTERNAL_STORAGE`
- `READ_EXTERNAL_STORAGE`

## Troubleshooting

### PDF Won't Export

- Ensure you have selected at least one entry
- Check that you have storage permissions (on mobile devices)
- Verify there's enough disk space

### PDF Won't Open

- Ensure you have a PDF reader installed
- Try manually opening the file from your Downloads folder

### Locked Entries Not Showing Content

- This is by design for privacy protection
- Locked entries will always show a protection message in PDFs
- To include full content, unlock entries before exporting

## Future Enhancements

Potential improvements for future versions:

- Custom PDF templates
- Export format options (A4, Letter, A5)
- Include/exclude locked entries option
- Custom cover page
- Table of contents for large exports
- Export statistics and charts
