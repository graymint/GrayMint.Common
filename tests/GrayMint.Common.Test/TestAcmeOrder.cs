using GrayMint.Common.Acme;

namespace GrayMint.Common.Test;

/// <summary>An order whose challenge stays pending a number of reads, then gives a certificate, or fails.</summary>
internal class TestAcmeOrder(string domain, int pendingCount = 0, Exception? failure = null) : IAcmeOrder
{
    public string Domain => domain;
    public string Token { get; } = $"token_{Guid.NewGuid():N}";
    public string KeyAuthorization { get; } = $"key_{Guid.NewGuid():N}";
    public int ValidateCount { get; private set; }

    public Task<byte[]?> Validate(CancellationToken cancellationToken)
    {
        ValidateCount++;
        return failure != null
            ? throw failure
            : Task.FromResult(ValidateCount > pendingCount ? AcmeTestUtil.CreatePfx(domain) : null);
    }
}
