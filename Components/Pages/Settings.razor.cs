using journalstart.Services;
using Microsoft.AspNetCore.Components;

namespace journalstart.Components.Pages;

public partial class Settings
{
    [Inject] private ThemeService ThemeService { get; set; } = default!;

    private bool HasPin => !string.IsNullOrEmpty(Preferences.Get("journal_pin", string.Empty));
    private bool ShowPinModal { get; set; }
    private string PinModalTitle { get; set; } = "Set PIN";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await ThemeService.InitializeAsync();
            StateHasChanged();
        }
    }

    private async Task SetTheme(string theme)
    {
        await ThemeService.SetThemeAsync(theme);
        StateHasChanged();
    }

    private string GetThemeButtonClass(string theme)
    {
        if (ThemeService.CurrentTheme == theme)
        {
            return "border-indigo-500 bg-indigo-50 dark:bg-indigo-900/20";
        }
        return "border-slate-200 bg-white hover:border-slate-300 dark:border-slate-700 dark:bg-slate-800 dark:hover:border-slate-600";
    }

    private void ShowSetPinModal()
    {
        PinModalTitle = "Set PIN";
        ShowPinModal = true;
    }

    private void ShowChangePinModal()
    {
        PinModalTitle = "Change PIN";
        ShowPinModal = true;
    }

    private async Task HandlePinSubmit(string pin)
    {
        await Task.Run(() => Preferences.Set("journal_pin", pin));
        ShowPinModal = false;
        await InvokeAsync(StateHasChanged);
    }

    private void HandlePinCancel()
    {
        ShowPinModal = false;
    }

    private void RemovePin()
    {
        Preferences.Remove("journal_pin");
        StateHasChanged();
    }
}

