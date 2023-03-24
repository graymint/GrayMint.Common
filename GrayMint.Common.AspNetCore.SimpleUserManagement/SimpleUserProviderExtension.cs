using GrayMint.Common.AspNetCore.SimpleRoleAuthorization;
using GrayMint.Common.AspNetCore.SimpleUserManagement.Persistence;
using GrayMint.Common.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NJsonSchema.Validation;

namespace GrayMint.Common.AspNetCore.SimpleUserManagement;

public static class SimpleUserProviderExtension
{
    public static void AddGrayMintSimpleUserProvider(this IServiceCollection services, Action<DbContextOptionsBuilder>? optionsAction = null)
    {
        services.AddDbContext<SimpleUserDbContext>(optionsAction);
        services.AddScoped<ISimpleUserProvider, SimpleUserProvider>();
        services.AddScoped<SimpleUserProvider>();
        services.AddScoped<SimpleRoleProvider>();
    }

    public static async Task UseGrayMintSimpleUserProvider(this WebApplication webApplication)
    {
        await using var scope = webApplication.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SimpleUserDbContext>();
        await EfCoreUtil.EnsureTablesCreated(dbContext.Database, SimpleUserDbContext.Schema, nameof(SimpleUserDbContext.Users));
        await UpdateRoles(scope);
    }

    private static async Task UpdateRoles(AsyncServiceScope scope)
    {
        // create roles
        var roleProvider = scope.ServiceProvider.GetRequiredService<SimpleRoleProvider>();
        var roleAuthOptions = scope.ServiceProvider.GetService<IOptions<SimpleRoleAuthOptions>>();
        if (roleAuthOptions?.Value.Roles == null) return;

        var roles = roleAuthOptions.Value.Roles;
        var dbRoles = await roleProvider.List();

        // update existing roles
        var updatedRoles = roles.Where(x => dbRoles.Any(y => x.RoleId == y.RoleId && x.RoleName != y.RoleName));
        foreach (var role in updatedRoles)
        {
            await roleProvider.Update(role.RoleId, new Dtos.RoleUpdateRequest
            {
                RoleName = new Common.Utils.Patch<string>(role.RoleName)
            });
        }

        // create roles that does not exists
        var newRoles = roles.Where(x => dbRoles.All(y => x.RoleId != y.RoleId));
        foreach (var role in newRoles)
        {
            await roleProvider.Create(new Dtos.RoleCreateRequest
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName
            });
        }
    }
}