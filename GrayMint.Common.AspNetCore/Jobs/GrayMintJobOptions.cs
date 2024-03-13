using GrayMint.Common.JobController;

namespace GrayMint.Common.AspNetCore.Jobs;

public class GrayMintJobOptions : JobOptions
{
    public bool ExecuteOnShutdown { get; init; }
}