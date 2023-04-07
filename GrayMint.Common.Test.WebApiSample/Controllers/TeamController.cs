using GrayMint.Common.AspNetCore.SimpleUserControllers.Controllers;
using GrayMint.Common.AspNetCore.SimpleUserControllers.Services;
using GrayMint.Common.Test.WebApiSample.Models;
using GrayMint.Common.Test.WebApiSample.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GrayMint.Common.Test.WebApiSample.Controllers;

[ApiController]
public class TeamController : AppTeamController<int, App>
{
    private readonly WebApiSampleDbContext _dbContext;

    public TeamController(
        TeamService teamService, 
        UserService userService, 
        WebApiSampleDbContext dbContext) : 
        base(teamService, userService)
    {
        _dbContext = dbContext;
    }

    protected override int ToAppIdDto(string appId) => int.Parse(appId);
    protected override async Task<IEnumerable<App>> GetApps(IEnumerable<int> appIds)
    {
        var ret = await _dbContext.Apps
            .Where(x=>appIds.Contains(x.AppId))
            .ToArrayAsync();
        return ret;
    }
}