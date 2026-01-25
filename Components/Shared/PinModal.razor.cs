using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace journalstart.Components.Shared;

public partial class PinModal
{
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public string Title { get; set; } = "Enter PIN";
    [Parameter] public bool IsSettingPin { get; set; }
    [Parameter] public EventCallback<string> OnSubmit { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private string Pin { get; set; } = string.Empty;
    private string ConfirmPin { get; set; } = string.Empty;
    private string ErrorMessage { get; set; } = string.Empty;

    private async Task HandleKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && Pin.Length == 4)
        {
            if (!IsSettingPin || ConfirmPin.Length == 4)
            {
                await Submit();
            }
        }
        else if (e.Key == "Escape")
        {
            await Cancel();
        }
    }

    private async Task Submit()
    {
        ErrorMessage = string.Empty;

        if (Pin.Length != 4)
        {
            ErrorMessage = "PIN must be 4 digits";
            return;
        }

        if (!Pin.All(char.IsDigit))
        {
            ErrorMessage = "PIN must contain only numbers";
            return;
        }

        if (IsSettingPin)
        {
            if (ConfirmPin.Length != 4)
            {
                ErrorMessage = "Please confirm your PIN";
                return;
            }

            if (Pin != ConfirmPin)
            {
                ErrorMessage = "PINs do not match";
                return;
            }
        }

        await OnSubmit.InvokeAsync(Pin);
        Pin = string.Empty;
        ConfirmPin = string.Empty;
        ErrorMessage = string.Empty;
    }

    private async Task Cancel()
    {
        Pin = string.Empty;
        ConfirmPin = string.Empty;
        ErrorMessage = string.Empty;
        await OnCancel.InvokeAsync();
    }

    private async Task HandleBackdropClick()
    {
        await Cancel();
    }
}
