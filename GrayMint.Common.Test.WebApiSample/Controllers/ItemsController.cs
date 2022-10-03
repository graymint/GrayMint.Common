using GrayMint.Common.AspNetCore.Auth.SimpleAuthorization;
using GrayMint.Common.Test.WebApiSample.Models;
using GrayMint.Common.Test.WebApiSample.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrayMint.Common.Test.WebApiSample.Controllers;

[ApiController]
[Route("/api/v{version:apiVersion}/apps/{appId}/items")]
public class ItemsController : ControllerBase
{
    private readonly WebApiSampleDbContext _dbContext;

    public ItemsController(WebApiSampleDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost]
    [Authorize(SimpleAuth.Policy, Roles = Roles.AppUser)]
    public async Task<Item> Create(int appId, string itemName)
    {
        var ret = await _dbContext.Items.AddAsync(new Item { AppId = appId, ItemName = itemName });
        return ret.Entity;
    }
}