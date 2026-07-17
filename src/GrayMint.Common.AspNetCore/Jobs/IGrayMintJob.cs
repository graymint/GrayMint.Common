namespace GrayMint.Common.AspNetCore.Jobs;

public interface IGrayMintJob
{
    ValueTask RunJob(CancellationToken cancellationToken);
}