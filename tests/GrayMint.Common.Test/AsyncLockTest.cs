using GrayMint.Common.Utils;

namespace GrayMint.Common.Test;

[TestClass]
public class AsyncLockTest
{
    [TestMethod]
    public async Task Instance_lock_should_block_second_caller_until_disposed()
    {
        var asyncLock = new AsyncLock();

        using (await asyncLock.LockAsync()) {
            using var second = await asyncLock.LockAsync(TimeSpan.FromMilliseconds(50));
            Assert.IsFalse(second.Succeeded, "second lock must time out while the first is held");
        }

        using var third = await asyncLock.LockAsync(TimeSpan.FromSeconds(5));
        Assert.IsTrue(third.Succeeded, "lock must be acquirable after the first is disposed");
    }

    [TestMethod]
    public async Task Named_lock_should_block_same_name_but_not_other_names()
    {
        var name = Guid.NewGuid().ToString();

        using (await AsyncLock.LockAsync(name)) {
            using var sameName = await AsyncLock.LockAsync(name, TimeSpan.FromMilliseconds(50));
            Assert.IsFalse(sameName.Succeeded, "same-name lock must time out while held");

            using var otherName = await AsyncLock.LockAsync(Guid.NewGuid().ToString(), TimeSpan.FromSeconds(5));
            Assert.IsTrue(otherName.Succeeded, "different-name lock must not be affected");
        }

        using var again = await AsyncLock.LockAsync(name, TimeSpan.FromSeconds(5));
        Assert.IsTrue(again.Succeeded, "lock must be acquirable after release");
    }
}
