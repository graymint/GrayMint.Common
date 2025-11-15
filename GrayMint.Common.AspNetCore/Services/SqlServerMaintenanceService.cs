using GrayMint.Common.AspNetCore.Jobs;
using Microsoft.EntityFrameworkCore;

namespace GrayMint.Common.AspNetCore.Services;

internal class SqlServerMaintenanceService<T>(
    T dbContext,
    ILogger<SqlServerMaintenanceService<T>> logger)
    : IGrayMintJob where T : DbContext
{
    public async ValueTask RunJob(CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Starting a Sql Maintenance job... DbContext: {DbContext}", dbContext.GetType());

            dbContext.Database.SetCommandTimeout(TimeSpan.FromHours(48));
            await dbContext.Database.ExecuteSqlRawAsync(GrayMintResource.SqlMaintenance, cancellationToken);

            logger.LogInformation("Sql Maintenance job has been finished. DbContext: {DbContext}", dbContext.GetType());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not complete a Maintenance job. DbContext: {DbContext}", dbContext.GetType());
        }

    }
}
