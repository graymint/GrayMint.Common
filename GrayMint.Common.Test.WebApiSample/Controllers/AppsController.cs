using GrayMint.Common.AspNetCore.SimpleRoleAuthorization;
using GrayMint.Common.Test.WebApiSample.Models;
using GrayMint.Common.Test.WebApiSample.Persistence;
using GrayMint.Common.Test.WebApiSample.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GrayMint.Common.Test.WebApiSample.Controllers;

[ApiController]
[Route("/api/v{version:apiVersion}/apps")]
public class AppsController : ControllerBase
{
    private readonly WebApiSampleDbContext _dbContext;

    public AppsController(WebApiSampleDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost]
    [AuthorizePermission(Permission.SystemWrite)]
    public async Task<App> CreateApp(string appName)
    {
        var ret = await _dbContext.Apps.AddAsync(new App { AppName = appName });
        await _dbContext.SaveChangesAsync();
        return ret.Entity;
    }

    [HttpGet]
    [AuthorizePermission(Permission.SystemRead)]
    public async Task<App[]> List()
    {
        var ret = await _dbContext.Apps.ToArrayAsync();
        await _dbContext.SaveChangesAsync();
        return ret;
    }
}