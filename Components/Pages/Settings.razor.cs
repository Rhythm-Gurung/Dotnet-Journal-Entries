using journalstart.Services;
using Microsoft.AspNetCore.Components;

namespace journalstart.Components.Pages;

public partial class Settings
{
    [Inject] private ThemeService ThemeService { get; set; } = default!;

    // PIN Lock Properties
    private bool isPinSet = false;
    private bool isModifying = false;
    private bool isDeleting = false;
    private string newPin = string.Empty;
    private string confirmPin = string.Empty;
    private string currentPin = string.Empty;
    private string deletePin = string.Empty;
    private string pinError = string.Empty;
    private string pinSuccess = string.Empty;

    private const string PIN_KEY = "journal_pin_lock";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await ThemeService.InitializeAsync();
            await CheckPinStatus();
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

    // PIN Lock Methods
    private async Task CheckPinStatus()
    {
        try
        {
            var storedPin = await SecureStorage.GetAsync(PIN_KEY);
            isPinSet = !string.IsNullOrEmpty(storedPin);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking PIN status: {ex.Message}");
            isPinSet = false;
        }
    }

    private async Task SetPin()
    {
        ClearMessages();

        if (!ValidatePin(newPin, confirmPin))
        {
            return;
        }

        try
        {
            await SecureStorage.SetAsync(PIN_KEY, newPin);
            isPinSet = true;
            pinSuccess = "PIN successfully set!";
            ClearPinInputs();

            // Clear success message after 3 seconds
            await Task.Delay(3000);
            pinSuccess = string.Empty;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            pinError = $"Failed to set PIN: {ex.Message}";
        }
    }

    private async Task UpdatePin()
    {
        ClearMessages();

        try
        {
            var storedPin = await SecureStorage.GetAsync(PIN_KEY);

            if (storedPin != currentPin)
            {
                pinError = "Current PIN is incorrect";
                return;
            }

            if (!ValidatePin(newPin, confirmPin))
            {
                return;
            }

            await SecureStorage.SetAsync(PIN_KEY, newPin);
            pinSuccess = "PIN successfully updated!";
            ClearPinInputs();
            isModifying = false;

            // Clear success message after 3 seconds
            await Task.Delay(3000);
            pinSuccess = string.Empty;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            pinError = $"Failed to update PIN: {ex.Message}";
        }
    }

    private async Task DeletePin()
    {
        ClearMessages();

        try
        {
            var storedPin = await SecureStorage.GetAsync(PIN_KEY);

            if (storedPin != deletePin)
            {
                pinError = "Incorrect PIN. Please try again.";
                return;
            }

            SecureStorage.Remove(PIN_KEY);
            isPinSet = false;
            isDeleting = false;
            pinSuccess = "PIN successfully deleted!";
            ClearPinInputs();

            // Clear success message after 3 seconds
            await Task.Delay(3000);
            pinSuccess = string.Empty;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            pinError = $"Failed to delete PIN: {ex.Message}";
        }
    }

    private void ShowModifyForm()
    {
        ClearMessages();
        ClearPinInputs();
        isModifying = true;
        isDeleting = false;
    }

    private void CancelModify()
    {
        ClearMessages();
        ClearPinInputs();
        isModifying = false;
    }

    private void ShowDeleteForm()
    {
        ClearMessages();
        ClearPinInputs();
        isDeleting = true;
        isModifying = false;
    }

    private void CancelDelete()
    {
        ClearMessages();
        ClearPinInputs();
        isDeleting = false;
    }

    private bool ValidatePin(string pin, string confirm)
    {
        if (string.IsNullOrWhiteSpace(pin))
        {
            pinError = "PIN cannot be empty";
            return false;
        }

        if (pin.Length != 4)
        {
            pinError = "PIN must be exactly 4 digits";
            return false;
        }

        if (!pin.All(char.IsDigit))
        {
            pinError = "PIN must contain only numbers";
            return false;
        }

        if (pin != confirm)
        {
            pinError = "PINs do not match";
            return false;
        }

        return true;
    }

    private void ClearPinInputs()
    {
        newPin = string.Empty;
        confirmPin = string.Empty;
        currentPin = string.Empty;
        deletePin = string.Empty;
    }

    private void ClearMessages()
    {
        pinError = string.Empty;
        pinSuccess = string.Empty;
    }
}

