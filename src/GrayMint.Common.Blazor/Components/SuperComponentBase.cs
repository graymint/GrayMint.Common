using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;
using GrayMint.Common.Blazor.Components.GenericInputs;
using GrayMint.Common.Blazor.Utils;

namespace GrayMint.Common.Blazor.Components;

public class SuperComponentBase : ComponentBase, IDisposable
{
    protected bool Disposed;
    protected bool IsBusy => WaitState.IsBusy;

    [Inject] protected ISnackbar Snackbar { get; set; } = null!;
    [Inject] protected IDialogService DialogService { get; set; } = null!;
    [Inject] public HttpClient Http { get; set; } = null!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = null!;
    [Inject] protected ILogger<SuperComponentBase> Logger { get; set; } = null!;
    [Parameter] public WaitState WaitState { get; set; } = new();
    [CascadingParameter] protected RouteData RouteData { get; set; } = null!;

    private bool _isWaitStateRegistered;
    public IDisposable SetBusy()
    {
        if (!_isWaitStateRegistered)
        {
            WaitState.IsBusyChanged += WaitState_IsBusyChanged;
            _isWaitStateRegistered = true;
        }

        return WaitState.Start();
    }

    private void WaitState_IsBusyChanged(object? sender, EventArgs e)
    {
        StateHasChanged();
    }

    public Task<bool> ShowWarningBox(WarningBoxParams warningBoxParams)
    {
        return WarningBox.ShowAsync(DialogService, warningBoxParams);
    }

    public Task<bool> ShowWarningBox(string? title = null, string? message = null, IEnumerable<string>? warnings = null)
    {
        return ShowWarningBox(new WarningBoxParams
        {
            Title = title,
            Warnings = warnings,
            Message = message
        });
    }

    // show an informational box with a single close button; it has no yes/no choice
    public Task ShowMessageBox(string? title = null, string? message = null, IEnumerable<string>? warnings = null)
    {
        return ShowWarningBox(new WarningBoxParams
        {
            Title = title,
            Warnings = warnings,
            Message = message,
            YesButtonText = CommonResource.Close,
            ShowNoButton = false
        });
    }

    public async Task<string?> ShowInputBox(string? value, string title, string? placeHolder = null, string? helperText = null)
    {
        var inputBox = await ShowInputBox(new InputBoxParams<string?>
        {
            Value = value,
            Title = title,
            PlaceHolder = placeHolder,
            HelperText = helperText,
            Required = true,
            CancelIfNoChange = false
        });

        return inputBox.Canceled ? null : inputBox.Value;
    }

    public Task<DialogReferenceExtensions.Result<T>> ShowInputBox<T>(InputBoxParams<T> parameters)
    {
        return GenericInputBox<T>.ShowAsync(DialogService, parameters);
    }

    public async Task ShowException(Exception ex, string? helpUrl = null)
    {
        StateHasChanged(); //let the UI update before showing the dialog
        if (ex is OperationCanceledException)
        {
            Logger.LogWarning("The operation has been cancelled.");
            return;
        }

        var dialog = await DialogService.ShowAsync<ErrorDialog>(CommonResource.Error,
            new DialogParameters { ["Error"] = ex, ["HelpUrl"] = helpUrl });

        await dialog.Result;
    }

    protected void NotifyItemUpdated(string itemName)
    {
        Snackbar.Add(string.Format(CommonResource.ItemUpdated, itemName), Severity.Success);
    }

    protected void NotifyItemAdded(string itemName)
    {
        Snackbar.Add(string.Format(CommonResource.ItemAdded, itemName), Severity.Success);
    }

    protected void NotifyItemRemoved(string itemName)
    {
        Snackbar.Add(string.Format(CommonResource.ItemRemoved, itemName), Severity.Success);
    }

    protected void NotifyError(string message)
    {
        Snackbar.Add(message, Severity.Error);
    }

    public virtual void Dispose()
    {
        WaitState.IsBusyChanged -= WaitState_IsBusyChanged;
        GC.SuppressFinalize(this);
    }
}