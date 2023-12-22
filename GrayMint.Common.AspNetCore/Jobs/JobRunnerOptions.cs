namespace GrayMint.Common.AspNetCore.Jobs;

public class JobRunnerOptions
{
    public int MaxDegreeOfParallelism { get; set; } = 1;
}