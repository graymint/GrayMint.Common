using GrayMint.Common.AspNetCore.SimpleRoleAuthorization;
using GrayMint.Common.Test.WebApiSample.Models;
using GrayMint.Common.Test.WebApiSample.Persistence;
using GrayMint.Common.Test.WebApiSample.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    [Authorize(SimpleRoleAuth.Policy, Roles = nameof(Roles.AppUser))]
    public async Task<Item> Create(int appId, string itemName)
    {
        var ret = await _dbContext.Items.AddAsync(new Item { AppId = appId, ItemName = itemName });
        await _dbContext.SaveChangesAsync();
        return ret.Entity;
    }

    [HttpPost("by-permission")]
    [AuthorizePermission(Permission.ItemWrite)]
    public async Task<Item> CreateByPermission(int appId, string itemName)
    {
        var ret = await _dbContext.Items.AddAsync(new Item { AppId = appId, ItemName = itemName });
        await _dbContext.SaveChangesAsync();
        return ret.Entity;
    }

    [HttpGet("itemId")]
    [Authorize(SimpleRoleAuth.Policy, Roles = $"{nameof(Roles.AppUser)},{nameof(Roles.AppReader)}")]
    public async Task<Item> Get(int appId, int itemId)
    {
        var ret = await _dbContext.Items.SingleAsync(x => x.AppId == appId && x.ItemId == itemId);
        return ret;
    }

    [HttpGet("itemId/by-permission")]
    [AuthorizePermission(Permission.ItemRead)]
    public async Task<Item> GetByPermission(int appId, int itemId)
    {
        var ret = await _dbContext.Items.SingleAsync(x => x.AppId == appId && x.ItemId == itemId);
        return ret;
    }


    [HttpDelete]
    [AuthorizePermission(Permission.ItemWrite)]
    public async Task DeleteByPermission(int appId, string itemName)
    {
        var item = await _dbContext.Items.SingleAsync(x => x.AppId == appId && x.ItemName == itemName);
        _dbContext.Items.Remove(item);
        await _dbContext.SaveChangesAsync();
    }

}