namespace GrayMint.Common.AspNetCore.ApplicationLifetime;

public interface IGrayMintApplicationLifetime
{
    public Task ApplicationStarted();
    public Task ApplicationStopping();
}