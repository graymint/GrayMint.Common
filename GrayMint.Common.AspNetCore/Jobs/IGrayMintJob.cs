namespace GrayMint.Common.AspNetCore.Jobs;

public interface IGrayMintJob
{
    Task RunJob(CancellationToken cancellationToken);
}