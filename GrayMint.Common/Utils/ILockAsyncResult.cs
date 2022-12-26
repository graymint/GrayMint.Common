namespace GrayMint.Common.Utils;

public interface ILockAsyncResult : IDisposable
{
    public bool Succeeded { get; }
}