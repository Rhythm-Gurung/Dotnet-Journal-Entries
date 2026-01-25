using Microsoft.JSInterop;

namespace journalstart.Services;

public class ThemeService
{
    private readonly IJSRuntime _jsRuntime;
    private string _currentTheme = "light";
    private Dictionary<string, string> _customColors = new();

    public event Action? OnThemeChanged;

    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public string CurrentTheme => _currentTheme;

    public Dictionary<string, string> CustomColors => _customColors;

    public async Task InitializeAsync()
    {
        try
        {
            // Load from Preferences first (native storage)
            var savedTheme = Preferences.Get("user_theme", "");
            if (!string.IsNullOrEmpty(savedTheme))
            {
                _currentTheme = savedTheme.ToLower();
            }
            else
            {
                // Fallback to localStorage for web
                var webTheme = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "journal-theme");
                if (!string.IsNullOrEmpty(webTheme))
                {
                    _currentTheme = webTheme;
                }
            }

            // Load custom colors from Preferences
            var savedColors = Preferences.Get("user_custom_colors", "");
            if (!string.IsNullOrEmpty(savedColors))
            {
                _customColors = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(savedColors) ?? new();
            }
            else
            {
                // Fallback to localStorage
                var webColors = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "journal-custom-colors");
                if (!string.IsNullOrEmpty(webColors))
                {
                    _customColors = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(webColors) ?? new();
                }
            }

            await ApplyTheme();
        }
        catch
        {
            // If initialization fails, use defaults
        }
    }

    public async Task SetThemeAsync(string theme)
    {
        _currentTheme = theme;

        // Save to Preferences (persistent native storage)
        Preferences.Set("user_theme", theme);

        // Also save to localStorage for web compatibility
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "journal-theme", theme);
        }
        catch { }

        await ApplyTheme();
        OnThemeChanged?.Invoke();
    }

    public async Task SetCustomColorAsync(string colorKey, string colorValue)
    {
        _customColors[colorKey] = colorValue;
        var json = System.Text.Json.JsonSerializer.Serialize(_customColors);

        // Save to Preferences (persistent native storage)
        Preferences.Set("user_custom_colors", json);

        // Also save to localStorage for web compatibility
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "journal-custom-colors", json);
        }
        catch { }

        if (_currentTheme == "custom")
        {
            await ApplyCustomColors();
        }

        OnThemeChanged?.Invoke();
    }

    private async Task ApplyTheme()
    {
        try
        {
            // Remove all theme classes first
            await _jsRuntime.InvokeVoidAsync("eval", @"
                document.documentElement.classList.remove('dark', 'light', 'custom');
            ");

            // Apply new theme
            if (_currentTheme == "dark")
            {
                await _jsRuntime.InvokeVoidAsync("eval", "document.documentElement.classList.add('dark')");
            }
            else if (_currentTheme == "custom")
            {
                await _jsRuntime.InvokeVoidAsync("eval", "document.documentElement.classList.add('custom')");
                await ApplyCustomColors();
            }
            else
            {
                await _jsRuntime.InvokeVoidAsync("eval", "document.documentElement.classList.add('light')");
            }
        }
        catch
        {
            // Silently fail if JS interop is not available
        }
    }

    private async Task ApplyCustomColors()
    {
        try
        {
            foreach (var (key, value) in _customColors)
            {
                await _jsRuntime.InvokeVoidAsync("eval",
                    $"document.documentElement.style.setProperty('--color-{key}', '{value}')");
            }
        }
        catch
        {
            // Silently fail
        }
    }

    public string GetCustomColor(string key)
    {
        return _customColors.TryGetValue(key, out var value) ? value : "";
    }
}
