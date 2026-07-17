using GrayMint.Common.Utils;

namespace GrayMint.Common.Test;

[TestClass]
public class GmUtilsTest
{
    private class Dto
    {
        public string Name { get; init; } = "";
        public int Age { get; init; }
    }

    [TestMethod]
    public void JsonDeserialize_should_return_object()
    {
        var dto = GmUtils.JsonDeserialize<Dto>("""{"Name":"foo","Age":10}""");
        Assert.AreEqual("foo", dto.Name);
        Assert.AreEqual(10, dto.Age);
    }

    [TestMethod]
    public void JsonDeserialize_should_throw_when_result_is_null()
    {
        Assert.ThrowsExactly<InvalidDataException>(() => GmUtils.JsonDeserialize<Dto>("null"));
    }

    [TestMethod]
    public void JsonClone_should_deep_copy()
    {
        var source = new Dto { Name = "foo", Age = 10 };
        var clone = GmUtils.JsonClone(source);
        Assert.AreNotSame(source, clone);
        Assert.AreEqual(source.Name, clone.Name);
        Assert.AreEqual(source.Age, clone.Age);
    }

    [TestMethod]
    public void RedactJsonValue_should_mask_given_keys_only()
    {
        const string json = """{"password":"secret","name":"bob"}""";
        var redacted = GmUtils.RedactJsonValue(json, ["password"]);
        StringAssert.Contains(redacted, "***");
        Assert.IsFalse(redacted.Contains("secret"), "password value must be redacted");
        StringAssert.Contains(redacted, "bob");
    }

    [TestMethod]
    public void SequenceEqualsOrNull_should_handle_nulls_and_sequences()
    {
        Assert.IsTrue(GmUtils.SequenceEqualsOrNull<int>(null, null));
        Assert.IsFalse(GmUtils.SequenceEqualsOrNull([1, 2], null));
        Assert.IsFalse(GmUtils.SequenceEqualsOrNull(null, [1, 2]));
        Assert.IsTrue(GmUtils.SequenceEqualsOrNull([1, 2, 3], [1, 2, 3]));
        Assert.IsFalse(GmUtils.SequenceEqualsOrNull([1, 2, 3], [1, 2]));
    }

    [TestMethod]
    public void IsNullOrEmpty_should_handle_nulls_and_items()
    {
        Assert.IsTrue(GmUtils.IsNullOrEmpty<int>(null));
        Assert.IsTrue(GmUtils.IsNullOrEmpty(Array.Empty<int>()));
        Assert.IsFalse(GmUtils.IsNullOrEmpty([1]));
    }

    [TestMethod]
    public void GenerateKey_should_return_key_of_requested_size()
    {
        Assert.HasCount(16, GmUtils.GenerateKey());
        Assert.HasCount(32, GmUtils.GenerateKey(256));
    }

    [TestMethod]
    public void TryInvoke_should_swallow_exception_and_return_default()
    {
        var result = GmUtils.TryInvoke("failing", int () => throw new InvalidOperationException(), -1);
        Assert.AreEqual(-1, result);

        var ok = GmUtils.TryInvoke("ok", () => 42);
        Assert.AreEqual(42, ok);
    }

    [TestMethod]
    public async Task TryInvokeAsync_should_swallow_exception_and_return_default()
    {
        var result = await GmUtils.TryInvokeAsync("failing", Task<int> () => throw new InvalidOperationException(), -1);
        Assert.AreEqual(-1, result);

        var ok = await GmUtils.TryInvokeAsync("ok", () => Task.FromResult(42));
        Assert.AreEqual(42, ok);
    }

    [TestMethod]
    public async Task ForEachAsync_should_process_all_items_within_parallelism_limit()
    {
        const int maxDegree = 3;
        var current = 0;
        var maxObserved = 0;
        var processed = 0;
        var items = Enumerable.Range(0, 20).ToArray();

        await GmUtils.ForEachAsync(items, async _ => {
            var now = Interlocked.Increment(ref current);
            InterlockedMax(ref maxObserved, now);
            await Task.Delay(10);
            Interlocked.Decrement(ref current);
            Interlocked.Increment(ref processed);
        }, maxDegree, CancellationToken.None);

        Assert.AreEqual(items.Length, processed);
        Assert.IsLessThanOrEqualTo(maxDegree, maxObserved);
    }

    private static void InterlockedMax(ref int location, int value)
    {
        int snapshot;
        do {
            snapshot = Volatile.Read(ref location);
            if (value <= snapshot) return;
        } while (Interlocked.CompareExchange(ref location, value, snapshot) != snapshot);
    }
}
