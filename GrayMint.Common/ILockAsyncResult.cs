namespace GrayMint.Common;

public interface ILockAsyncResult : IDisposable
{
    public bool Succeeded { get; }
}