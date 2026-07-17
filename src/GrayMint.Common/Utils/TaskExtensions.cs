using System.Runtime.CompilerServices;

namespace GrayMint.Common.Utils;

public static class TaskExtensions
{
    public static bool DefaultContinueOnCapturedContext { get; set; }

    public static ConfiguredTaskAwaitable Vhc(this Task task)
    {
        return task.ConfigureAwait(DefaultContinueOnCapturedContext);
    }

    public static ConfiguredTaskAwaitable<T> Vhc<T>(this Task<T> task)
    {
        return task.ConfigureAwait(DefaultContinueOnCapturedContext);
    }

    public static ConfiguredValueTaskAwaitable Vhc(this ValueTask task)
    {
        return task.ConfigureAwait(DefaultContinueOnCapturedContext);
    }

    public static ConfiguredValueTaskAwaitable<T> Vhc<T>(this ValueTask<T> task)
    {
        return task.ConfigureAwait(DefaultContinueOnCapturedContext);
    }

    public static void VhBlock(this ValueTask task)
    {
        if (!task.IsCompleted)
            task.GetAwaiter().GetResult();
    }

    extension(CancellationTokenSource cancellationTokenSource)
    {
        public Exception? TryCancel()
        {
            try
            {
                if (!cancellationTokenSource.IsCancellationRequested)
                    cancellationTokenSource.Cancel();
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        public async Task<Exception?> TryCancelAsync()
        {
            try
            {
                if (!cancellationTokenSource.IsCancellationRequested)
                    await cancellationTokenSource.CancelAsync().Vhc();
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
    }
}