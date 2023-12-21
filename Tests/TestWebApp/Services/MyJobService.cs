using GrayMint.Common.AspNetCore.Jobs;

namespace TestWebApp.Services;

public class MyJobService : IGrayMintJob
{
    public async Task RunJob(CancellationToken cancellationToken)
    {
        Console.WriteLine("MyJobService Started");
        await Task.Delay(3000, cancellationToken);
        Console.WriteLine("MyJobService Completed.");
    }
}

public class MyJobService2 : IGrayMintJob
{
    public async Task RunJob(CancellationToken cancellationToken)
    {
        Console.WriteLine("MyJobService2 Started");
        await Task.Delay(3000, cancellationToken);
        Console.WriteLine("MyJobService2 Completed.");
    }
}