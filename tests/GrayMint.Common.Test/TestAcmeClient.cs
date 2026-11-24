using GrayMint.Common.Acme;

namespace GrayMint.Common.Test;

internal class TestAcmeClient(Func<string, TestAcmeOrder> createOrder) : IAcmeClient
{
    public List<TestAcmeOrder> Orders { get; } = [];

    public Task<string> CreateAccount(CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"account_{Guid.NewGuid():N}");
    }

    public Task<IAcmeOrder> CreateOrder(string accountKeyPem, string domain, CancellationToken cancellationToken = default)
    {
        var order = createOrder(domain);
        Orders.Add(order);
        return Task.FromResult<IAcmeOrder>(order);
    }
}
