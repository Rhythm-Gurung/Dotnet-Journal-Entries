# Theme System Documentation

## Overview

The app now supports Light, Dark, and Custom themes using Tailwind CSS dark mode and CSS variables.

## How It Works

### 1. Theme Service (`Services/ThemeService.cs`)

- Manages theme state (light/dark/custom)
- Persists theme choice in localStorage
- Applies theme classes to `<html>` element
- Handles custom color CSS variables

### 2. Tailwind Configuration (`tailwind.config.js`)

```javascript
darkMode: 'class',  // Enables class-based dark mode
```

### 3. CSS Variables (`wwwroot/css/tailwind-input.css`)

- `:root` - Light theme colors
- `.dark` - Dark theme colors
- `.custom` - Custom theme (uses CSS variables set by JS)

## Using Themes in Components

### Method 1: Tailwind Dark Mode Classes (Recommended)

```html
<!-- Add dark: variants to existing classes -->
<div class="bg-white text-slate-800 dark:bg-slate-800 dark:text-slate-100">
  Content
</div>

<button
  class="bg-indigo-100 text-indigo-900 hover:bg-indigo-200 
               dark:bg-indigo-900 dark:text-indigo-200 dark:hover:bg-indigo-800"
>
  Button
</button>
```

### Method 2: CSS Variables (For Custom Theme Support)

```html
<!-- Use themed utility classes defined in tailwind-input.css -->
<div class="themed-bg themed-text themed-border">Content</div>

<!-- Or use arbitrary values with CSS variables -->
<div
  class="bg-[rgb(var(--color-bg-card))] text-[rgb(var(--color-text-primary))]"
>
  Content
</div>
```

## Available CSS Variables

### Background Colors

- `--color-bg-primary` - Main background
- `--color-bg-secondary` - Secondary background
- `--color-bg-tertiary` - Tertiary background
- `--color-bg-accent` - Accent background
- `--color-bg-card` - Card background

### Text Colors

- `--color-text-primary` - Primary text
- `--color-text-secondary` - Secondary text
- `--color-text-tertiary` - Tertiary text
- `--color-text-accent` - Accent text

### Border Colors

- `--color-border-primary` - Primary borders
- `--color-border-secondary` - Secondary borders
- `--color-border-accent` - Accent borders

### Accent Colors

- `--color-accent-primary` - Primary accent
- `--color-accent-hover` - Accent hover state
- `--color-accent-light` - Light accent

## Components Already Updated

✅ NavMenu.razor - Dark mode support added
✅ Settings.razor - Theme selector with custom colors
✅ tailwind-input.css - CSS variables and dark variants

## Components That Need Updates

To fully support theming, the following components should be updated with `dark:` variants:

- [ ] Today.razor
- [ ] Timeline.razor
- [ ] Insights.razor
- [ ] Analytics.razor
- [ ] MarkdownEditor.razor
- [ ] MoodSelector.razor
- [ ] MainLayout.razor

## How to Update a Component

1. **Find hardcoded colors** (slate-_, indigo-_, etc.)
2. **Add dark: variants** to each color class
3. **Test in both themes** using Settings page

Example:

```html
<!-- Before -->
<div class="bg-white border-slate-200 text-slate-800">
  <!-- After -->
  <div
    class="bg-white border-slate-200 text-slate-800 
            dark:bg-slate-800 dark:border-slate-700 dark:text-slate-100"
  ></div>
</div>
```

## Quick Reference: Common Color Mappings

| Light Mode         | Dark Mode               |
| ------------------ | ----------------------- |
| `bg-white`         | `dark:bg-slate-800`     |
| `bg-slate-50`      | `dark:bg-slate-900`     |
| `bg-slate-100`     | `dark:bg-slate-800`     |
| `text-slate-800`   | `dark:text-slate-100`   |
| `text-slate-600`   | `dark:text-slate-300`   |
| `text-slate-400`   | `dark:text-slate-500`   |
| `border-slate-200` | `dark:border-slate-700` |
| `border-slate-300` | `dark:border-slate-600` |
| `bg-indigo-100`    | `dark:bg-indigo-900/50` |
| `text-indigo-900`  | `dark:text-indigo-200`  |

## Custom Theme Colors

When users select "Custom" theme and pick colors, the ThemeService converts HEX colors to RGB and sets CSS variables:

```javascript
// User picks #ff0000 for bg-primary
// Service sets: --custom-bg-primary: 255 0 0
// CSS uses: rgb(var(--custom-bg-primary))
```

## Testing

1. Go to Settings page
2. Click Light/Dark/Custom theme buttons
3. For Custom: Pick colors and see preview update
4. Navigate to other pages to verify theme persistence
5. Refresh page - theme should be remembered (localStorage)
