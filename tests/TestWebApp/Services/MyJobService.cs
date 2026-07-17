using GrayMint.Common.AspNetCore.Jobs;

namespace TestWebApp.Services;

public class MyJobService : IGrayMintJob
{
    public async ValueTask RunJob(CancellationToken cancellationToken)
    {
        Console.WriteLine("MyJobService Started");
        await Task.Delay(3000, cancellationToken);
        Console.WriteLine("MyJobService Completed.");
    }
}