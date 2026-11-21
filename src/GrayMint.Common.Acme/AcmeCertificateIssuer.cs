using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GrayMint.Common.Acme;

/// <summary>A challenge's token and what its URL must answer, while the authority checks it.</summary>
public record AcmeChallenge(string Domain, string Token, string KeyAuthorization);

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

/// <summary>
/// Gets a certificate for one domain over the HTTP-01 challenge, from the order to the certificate: what is done with
/// the challenge in between, and where the certificate is kept, is the app's.
/// </summary>
public class AcmeCertificateIssuer(IAcmeClient acmeClient, ILogger<AcmeCertificateIssuer>? logger = null)
{
    private readonly ILogger _logger = logger ?? NullLogger<AcmeCertificateIssuer>.Instance;

    /// <summary>Opens a new account at the authority, which every later order needs; it is kept by the app.</summary>
    /// <returns>The account's key, as PEM.</returns>
    public Task<string> CreateAccount(CancellationToken cancellationToken = default)
    {
        return acmeClient.CreateAccount(cancellationToken);
    }

    /// <summary>Orders the certificate, publishes the challenge, checks it, and waits for the authority to sign.</summary>
    /// <returns>
    /// The certificate, as PFX bytes with no password: its key, the certificate and the authority's intermediates.
    /// </returns>
    /// <exception cref="AcmeSelfCheckException">Our own read of the challenge's URL failed.</exception>
    /// <exception cref="AcmeTimeoutException">The authority did not decide in time.</exception>
    /// <exception cref="AcmeException">The authority refused the challenge.</exception>
    public async Task<byte[]> Issue(AcmeIssueRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Domain);
        _logger.LogInformation("Ordering a certificate. Domain: {Domain}", request.Domain);
        var order = await acmeClient.CreateOrder(request.AccountKeyPem, request.Domain, cancellationToken);
        var challenge = new AcmeChallenge(request.Domain, order.Token, order.KeyAuthorization);

        // published inside, so a publish that kept the challenge and then failed still takes it away
        try
        {
            await request.PublishChallenge(challenge, cancellationToken);
            if (request.SelfCheckHandlerFactory != null)
            {
                using var handler = request.SelfCheckHandlerFactory();
                await AcmeSelfCheck.Check(request.Domain, order.Token, order.KeyAuthorization, handler,
                    request.SelfCheckTimeout, cancellationToken);
            }

            var pfx = await WaitForCertificate(order, request, cancellationToken);
            _logger.LogInformation("The certificate has been issued. Domain: {Domain}", request.Domain);
            return pfx;
        }
        finally
        {
            await Withdraw(request, challenge);
        }
    }

    private static async Task<byte[]> WaitForCertificate(IAcmeOrder order, AcmeIssueRequest request,
        CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(request.ValidateTimeout);
        try
        {
            while (true)
            {
                var pfx = await order.Validate(timeoutSource.Token);
                if (pfx != null)
                    return pfx;

                await Task.Delay(request.PollInterval, timeoutSource.Token);
            }
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AcmeTimeoutException(
                $"The certificate authority did not check the challenge of {request.Domain} within {request.ValidateTimeout}.",
                ex);
        }
    }

    // after the order's end, which a cancellation does not undo
    private async Task Withdraw(AcmeIssueRequest request, AcmeChallenge challenge)
    {
        if (request.WithdrawChallenge == null)
            return;

        try
        {
            await request.WithdrawChallenge(challenge, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not withdraw a challenge. Domain: {Domain}", challenge.Domain);
        }
    }
}
