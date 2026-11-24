namespace GrayMint.Common.Test;

// Every test class derives from it, for the test's cancellation token, which a test passes to what it calls.
public abstract class TestBase
{
    public required TestContext TestContext { get; set; }

    protected CancellationToken TestCt => TestContext.CancellationToken;
}
