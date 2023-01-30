namespace GrayMint.Common.JobController;

public interface IJob
{
    public Task RunJob();
    public JobSection? JobSection { get; } 
}