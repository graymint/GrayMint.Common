namespace GrayMint.Common.AspNetCore.ApplicationLifetime;

public interface IGrayMintApplicationLifetime
{
    public Task ApplicationStarted(CancellationToken cancellationToken);
    public Task ApplicationStopping(CancellationToken cancellationToken);
}