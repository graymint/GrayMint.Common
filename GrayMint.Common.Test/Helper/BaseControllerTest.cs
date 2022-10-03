using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GrayMint.Common.Test.Helper;

public abstract class BaseControllerTest
{
    protected TestInit TestInit1 { get; private set; } = default!;

    [TestInitialize]
    public virtual async Task Init()
    {
        TestInit1 = await TestInit.Create();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (TestInit1 != null!)
            TestInit1.Dispose();
    }

}