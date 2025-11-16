using GrayMint.Common.Jobs;

namespace GrayMint.Common.AspNetCore.Jobs;

public record GrayMintJobOptions : JobOptions
{
    public bool ExecuteOnShutdown { get; init; }
}