# JournalStart

A cross-platform journaling app built with .NET MAUI and Blazor. It lets you write daily entries, track your mood, and tag your thoughts, all in a modern, mobile-friendly interface.

## Overview

JournalStart is a .NET MAUI Blazor Hybrid application:

- The native shell is a .NET MAUI `ContentPage` hosting a `BlazorWebView`.
- The UI and navigation are implemented with Blazor components.
- Styling is handled primarily through Tailwind CSS.
- Entries are currently stored in-memory via a simple service layer (no database yet).

This structure makes it easy to evolve from a prototype into a more fully featured app (e.g., add persistence, sync, richer analytics).

## Features

- **Today view** (`Components/Pages/Today.razor`)

  - Create, edit, and delete a journal entry for a specific day.
  - Rich-text/markdown-style editor with a formatting toolbar.
  - Primary mood selection with emoji and category (Positive/Neutral/Negative).
  - Up to two secondary moods to capture nuance.
  - Free-form tags for organization.
  - Basic metadata display (created/updated timestamps).

- **Timeline view** (`Components/Pages/Timeline.razor`)

  - Placeholder page where browsing and filtering journal history will live.

- **Insights view** (`Components/Pages/Insights.razor`)

  - Placeholder page for analytics and trends over time.

- **Settings view** (`Components/Pages/Settings.razor`)

  - Placeholder page for configuring app preferences.

- **Mood picker** (`Components/Shared/MoodSelector.razor`)

  - Primary mood selection organized by mood category.
  - Secondary moods (up to two) selectable from the full catalog.
  - Visual summary of the selected primary and secondary moods.

- **Markdown-style editor** (`Components/Shared/MarkdownEditor.razor`)
  - Content-editable area with a simple formatting toolbar (bold, italic, headings, lists, links).
  - JavaScript interop module (`wwwroot/js/markdownEditor.js`) to execute formatting commands and keep the Blazor model in sync.
  - Local word count calculation based on rendered HTML.

## Architecture

### Entry point and hosting

- **MauiProgram** (`MauiProgram.cs`)

  - Configures the .NET MAUI app, fonts, logging, and dependency injection.
  - Registers the Blazor WebView and the `JournalService` as a singleton.

- **MainPage** (`MainPage.xaml`, `MainPage.xaml.cs`)

  - Native .NET MAUI `ContentPage` that hosts a `BlazorWebView`.
  - The web view uses `wwwroot/index.html` as the host page and boots the Blazor side.
  - Root component is `Components/Routes.razor` (attached to the `#app` element).

- **Host page** (`wwwroot/index.html`)
  - Standard Blazor host page including Tailwind CSS and the generated `journalstart.styles.css`.
  - Contains the `div#app` element where the Blazor app is rendered.

### Routing and layout

- **Routes** (`Components/Routes.razor`)

  - Uses the Blazor `Router` component to discover and navigate between pages.
  - Uses `Layout/MainLayout.razor` as the default layout.

- **Layout** (`Components/Layout/MainLayout.razor`, `Components/Layout/NavMenu.razor`)
  - Provides the app chrome (top bar and navigation) and renders `@Body` as the current page.

### Domain model

- **JournalEntry** (`Models/JournalEntry.cs`)

  - Represents a single day’s entry.
  - Key properties:
    - `Id`: unique identifier.
    - `Date`: the calendar date (using `DateOnly`).
    - `Content`: the entry body as rich HTML/markdown-like text.
    - `PrimaryMood`: primary mood id string.
    - `SecondaryMoods`: list of secondary mood id strings.
    - `Tags`: list of arbitrary tags.
    - `CreatedAt`, `UpdatedAt`: timestamps (UTC).

- **Mood & MoodCatalog** (`Models/Mood.cs`)
  - `MoodCategory`: Positive, Neutral, Negative.
  - `Mood` record: id, display name, emoji, and category.
  - `MoodCatalog` provides:
    - `All`: list of all moods.
    - `ByCategory`: moods grouped by category.
    - `ById`: lookup dictionary by id.
    - `Get(string? id)`: helper to safely retrieve a `Mood` by id.

### Services

- **JournalService** (`Services/JournalService.cs`)
  - In-memory storage for `JournalEntry` instances keyed by `DateOnly`.
  - Thread-safe access using a private lock.
  - Main methods:
    - `GetEntryAsync(DateOnly date)`: returns an entry for the given date, or `null`.
    - `UpsertEntryAsync(DateOnly date, string content, string? primaryMood, List<string> secondaryMoods, List<string> tags)`: creates or updates an entry and returns the saved object.
    - `DeleteEntryAsync(DateOnly date)`: removes an entry for the date and returns whether it existed.
  - Intended as a simple starting point; you can later swap this for a database or local storage implementation.

## How the Today page works

The Today page (`Components/Pages/Today.razor`) coordinates the UI components and the `JournalService`:

- Tracks the currently selected date, content, moods, tags, and metadata fields in component state.
- On initialization, calls `JournalService.GetEntryAsync(SelectedDate)` to load an existing entry (if any).
- Binds the entry content to `MarkdownEditor` via `@bind-Value`.
- Binds primary and secondary moods to `MoodSelector` via `@bind-PrimaryMood` and `@bind-SecondaryMoods`.
- Handles tag input (add/remove) and key events.
- Computes `CanSave` and `HasChanges` to avoid unnecessary saves and to enforce basic validation (content + primary mood required).
- Calls `JournalService.UpsertEntryAsync` to save and updates local state and status messages.
- Supports date navigation (previous/next day) by updating `SelectedDate` and reloading the entry.

## Running the app

### Prerequisites

- .NET 9 SDK (or the version this project targets).
- .NET MAUI workload installed.
- An IDE such as Visual Studio (with .NET MAUI workload) or VS Code with appropriate extensions.

### From Visual Studio

1. Open `journalstart.sln`.
2. Select a target platform (Android, iOS, Windows, MacCatalyst).
3. Press Run/Debug to deploy and launch the app.

### From the command line

From the project root (where `journalstart.csproj` is located):

```bash
# Restore dependencies
 dotnet restore

# Run for a specific target (example for Windows)
 dotnet build
 # Then run using your chosen MAUI target/profile from your IDE or CLI
```

Exact CLI commands depend on your installed workloads and platforms; Visual Studio is usually the easiest way to run and debug MAUI apps.

## Styling

- Tailwind configuration files:
  - `tailwind.config.js`
  - `styles/app.tailwind.css` (source for Tailwind styles).
- Generated CSS lives under `wwwroot/css` (e.g., `tailwind.css`, `app.css`) and is referenced by `wwwroot/index.html`.
- Component-specific styles may exist alongside razor files (e.g., `Components/Layout/MainLayout.razor.css`, `Components/Layout/NavMenu.razor.css`, `Components/Shared/MoodSelector.razor.css`).

## Extending the app

Here are some natural next steps you might document and implement:

- **Persistence**

  - Replace `JournalService`'s in-memory dictionary with a persistent store (e.g., SQLite, file-based storage, or secure local storage per platform).

- **Timeline**

  - Implement listing/filtering of entries over time using the data managed by `JournalService`.

- **Insights**

  - Add charts and summaries, such as mood distribution over time, word counts, or streaks.

- **Settings**
  - Add options for themes, default landing page, reminders, backup/export, etc.

## Documentation notes

This README is meant as a high-level guide for developers and for your own reference. If you want, we can also:

- Add XML documentation comments to the C# models and services.
- Create per-page documentation explaining user flows and UX decisions.
- Document the Tailwind build process and any custom JS interop in more depth.
