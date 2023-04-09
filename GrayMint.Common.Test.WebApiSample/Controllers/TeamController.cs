using GrayMint.Common.AspNetCore.SimpleUserControllers.Controllers;
using GrayMint.Common.AspNetCore.SimpleUserControllers.Services;
using GrayMint.Common.Test.WebApiSample.Models;
using GrayMint.Common.Test.WebApiSample.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GrayMint.Common.Test.WebApiSample.Controllers;

[ApiController]
public class TeamController : TeamControllerBase<App, int>
{
    private readonly WebApiSampleDbContext _dbContext;

    public TeamController(
        RoleService roleService,
        WebApiSampleDbContext dbContext) :
        base(roleService)
    {
        _dbContext = dbContext;
    }

    protected override string ToResourceId(int appId)
    {
        return appId == 0 ? "*" : appId.ToString();
    }

    protected override async Task<IEnumerable<App>> GetResources(string[] resourceIds)
    {
        var allApps = resourceIds.Contains("*");
        var ret = await _dbContext.Apps
            .Where(x => allApps || resourceIds.Contains(x.AppId.ToString()))
            .ToArrayAsync();
        return ret;
    }
}