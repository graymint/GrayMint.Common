using GrayMint.Common.AspNetCore.Jobs;

namespace TestWebApp.Services;

public class MyJobService2 : IGrayMintJob
{
    public async ValueTask RunJob(CancellationToken cancellationToken)
    {
        Console.WriteLine("MyJobService2 Started");
        await Task.Delay(3000, cancellationToken);
        Console.WriteLine("MyJobService2 Completed.");
    }
}