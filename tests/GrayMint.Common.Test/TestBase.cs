namespace GrayMint.Common.Test;

// Every test class derives from it, for the test's cancellation token, which a test names only where a call of its
// requires one; a helper's token defaults to None.
public abstract class TestBase
{
    public required TestContext TestContext { get; set; }

    protected CancellationToken TestCt => TestContext.CancellationToken;
}
