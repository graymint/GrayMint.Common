namespace GrayMint.Common.Acme;

public class AcmeIssueRequest
{
    /// <summary>The account's key, from <see cref="IAcmeClient.CreateAccount"/>.</summary>
    public required string AccountKeyPem { get; init; }

    public required string Domain { get; init; }

    /// <summary>
    /// Puts the challenge where whatever answers port 80 of the domain reads it, such as the database, before anyone
    /// reads its URL.
    /// </summary>
    public required Func<AcmeChallenge, CancellationToken, Task> PublishChallenge { get; init; }

    /// <summary>Takes the challenge away once the order has ended, whatever its end.</summary>
    public Func<AcmeChallenge, CancellationToken, Task>? WithdrawChallenge { get; init; }

    /// <summary>
    /// Makes the connection for our own read of the challenge's URL, before the authority is asked (see
    /// <see cref="AcmeSelfCheck"/>); <see langword="null"/> skips it. The domain is the user's, so it should reach public
    /// addresses only and follow no redirect. It is disposed after the read.
    /// </summary>
    public Func<HttpMessageHandler>? SelfCheckHandlerFactory { get; init; }

    public TimeSpan SelfCheckTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>How long the authority may take to check the challenge; Let's Encrypt takes seconds.</summary>
    public TimeSpan ValidateTimeout { get; init; } = TimeSpan.FromMinutes(2);

    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(5);
}
