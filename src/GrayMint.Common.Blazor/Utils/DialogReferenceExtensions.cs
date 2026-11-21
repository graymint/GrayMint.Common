using MudBlazor;

namespace GrayMint.Common.Blazor.Utils;

public static class DialogReferenceExtensions
{
    public class Result<T>
    {
        public required bool Canceled { get; init; }
        public required T? Value { get; init; }
        public T RequiredValue => Value ?? throw new Exception("Dialog has not returned any value.");
    }

    extension(IDialogReference dialogService)
    {
        public async Task<Result<T>> GetDialogResultAsync<T>()
        {
            var result = await dialogService.Result;
            return new Result<T>
            {
                Value = result?.Data is null ? default : (T)result.Data,
                Canceled = result is { Canceled: true }
            };
        }

        public async Task<bool> IsDialogCanceledAsync()
        {
            var result = await dialogService.Result;
            return result is { Canceled: true };
        }
    }
}