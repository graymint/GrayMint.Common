using GrayMint.Common.AspNetCore.Auth.SimpleAuthorization;
using GrayMint.Common.Test.WebApiSample.Models;
using GrayMint.Common.Test.WebApiSample.Persistence;
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
    [Authorize(SimpleAuth.Policy, Roles = Roles.AppCreator)]
    public async Task<App> CreateApp(string appName)
    {
        var ret = await _dbContext.Apps.AddAsync(new App { AppName = appName });
        return ret.Entity;
    }

    [HttpGet]
    [Authorize(SimpleAuth.Policy, Roles = $"{Roles.SystemAdmin},{Roles.AppCreator}")]
    public async Task<App[]> List()
    {
        var ret = await _dbContext.Apps.ToArrayAsync();
        return ret;
    }
}