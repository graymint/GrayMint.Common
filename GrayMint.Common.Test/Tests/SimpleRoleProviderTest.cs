using GrayMint.Common.AspNetCore.SimpleUserManagement;
using GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;
using GrayMint.Common.Exceptions;
using GrayMint.Common.Test.Helper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GrayMint.Common.Test.Tests;

[TestClass]
public class SimpleRoleProviderTest : BaseControllerTest
{

    [TestMethod]
    public async Task Crud()
    {
        // Create
        var roleProvider = TestInit1.CurrentServiceScope.ServiceProvider.GetRequiredService<SimpleRoleProvider>();
        var request = new RoleCreateRequest(Guid.NewGuid().ToString())
        {
            RoleName = Guid.NewGuid().ToString(),
            Description = Guid.NewGuid().ToString()
        };

        var role = await roleProvider.Create(request);
        Assert.AreEqual(request.RoleName, role.RoleName);
        Assert.AreEqual(request.Description, role.Description);
        Assert.AreNotEqual(0, role.RoleId);

        // Get
        var role2 = await roleProvider.Get(role.RoleId);
        Assert.AreEqual(role.RoleId, role2.RoleId);
        Assert.AreEqual(role.RoleName, role2.RoleName);
        Assert.AreEqual(role.Description, role2.Description);

        var role3 = await roleProvider.GetByName(role.RoleName);
        Assert.AreEqual(role.RoleId, role3.RoleId);
        Assert.AreEqual(role.RoleName, role3.RoleName);

        // Update
        var updateRequest = new RoleUpdateRequest()
        {
            RoleName = Guid.NewGuid().ToString(),
            Description = Guid.NewGuid().ToString()
        };
        await roleProvider.Update(role.RoleId, updateRequest);

        // Get
        var role4 = await roleProvider.Get(role.RoleId);
        Assert.AreEqual(role4.RoleName, updateRequest.RoleName.Value);
        Assert.AreEqual(role4.Description, updateRequest.Description.Value);

        // Remove
        await roleProvider.Remove(role.RoleId);
        try
        {
            await roleProvider.Get(role.RoleId);
            Assert.Fail("NotExistsException was expected.");
        }
        catch (Exception ex)
        {
            Assert.AreEqual("Sequence contains no elements.", ex.Message);
        }
    }

    [TestMethod]
    public async Task Add_remove_user()
    {
        // create a user
        var simpleUserProvider = TestInit1.CurrentServiceScope.ServiceProvider.GetRequiredService<SimpleUserProvider>();
        var userCreateRequest = new UserCreateRequest($"{Guid.NewGuid()}@local")
        {
            FirstName = Guid.NewGuid().ToString(),
            LastName = Guid.NewGuid().ToString(),
            Description = Guid.NewGuid().ToString()
        };
        var user = await simpleUserProvider.Create(userCreateRequest);

        // create a role
        var roleProvider = TestInit1.CurrentServiceScope.ServiceProvider.GetRequiredService<SimpleRoleProvider>();
        var role = await roleProvider.Create(new RoleCreateRequest(Guid.NewGuid().ToString())
        {
            RoleName = Guid.NewGuid().ToString(),
            Description = Guid.NewGuid().ToString()
        });
        await roleProvider.Create(new RoleCreateRequest(Guid.NewGuid().ToString())
        {
            RoleName = Guid.NewGuid().ToString(),
            Description = Guid.NewGuid().ToString()
        });

        // Add the user to roles
        await roleProvider.AddUser(role.RoleId, user.UserId, "*");
        await roleProvider.AddUser(role.RoleId, user.UserId, "1");
        try
        {
            await roleProvider.AddUser(role.RoleId, user.UserId, "*");
            Assert.Fail("AlreadyExistsException was expected.");
        }
        catch (Exception ex)
        {
            Assert.IsTrue(AlreadyExistsException.Is(ex));
        }

        // Check user Roles
        var userRoles = await roleProvider.GetUserRolesByUser(user.UserId);
        Assert.AreEqual(2, userRoles.Length);
        Assert.IsTrue(userRoles.Any(x => x.AppId == "*" && x.Role.RoleName == role.RoleName));
        Assert.IsTrue(userRoles.Any(x => x.AppId == "1" && x.Role.RoleName == role.RoleName));

        // Check user Roles
        userRoles = await roleProvider.GetUserRoles(role.RoleId);
        Assert.AreEqual(2, userRoles.Length);
        Assert.IsTrue(userRoles.Any(x => x.AppId == "*" && x.Role.RoleName == role.RoleName));
        Assert.IsTrue(userRoles.Any(x => x.AppId == "1" && x.Role.RoleName == role.RoleName));

        // Remove
        await roleProvider.RemoveUser(role.RoleId, user.UserId, "*");
        try
        {
            await roleProvider.RemoveUser(role.RoleId, user.UserId, "*");
            Assert.Fail("NotExistsException was expected.");
        }
        catch (Exception ex)
        {
            Assert.IsTrue(NotExistsException.Is(ex));
        }
    }

    [TestMethod]
    public async Task GetAuthUser()
    {
        // create a user
        var userProvider = TestInit1.CurrentServiceScope.ServiceProvider.GetRequiredService<SimpleUserProvider>();
        var user = await userProvider.Create(new UserCreateRequest($"{Guid.NewGuid()}@local")
        {
            FirstName = Guid.NewGuid().ToString(),
            LastName = Guid.NewGuid().ToString(),
            Description = Guid.NewGuid().ToString()
        });

        // create roles
        var roleProvider = TestInit1.CurrentServiceScope.ServiceProvider.GetRequiredService<SimpleRoleProvider>();
        var role1 = await roleProvider.Create(new RoleCreateRequest(Guid.NewGuid().ToString())
        {
            RoleName = Guid.NewGuid().ToString(),
            Description = Guid.NewGuid().ToString()
        });
        var role2 = await roleProvider.Create(new RoleCreateRequest(Guid.NewGuid().ToString())
        {
            RoleName = Guid.NewGuid().ToString(),
            Description = Guid.NewGuid().ToString()
        });

        // Add the user to roles
        await roleProvider.AddUser(role1.RoleId, user.UserId, "*");
        await roleProvider.AddUser(role1.RoleId, user.UserId, "1");
        await roleProvider.AddUser(role2.RoleId, user.UserId, "1");

        var authUser = await userProvider.FindSimpleUserByEmail(user.Email);
        Assert.IsNotNull(authUser);
        Assert.AreEqual(user.AuthCode, authUser.AuthorizationCode);
        Assert.AreEqual(3, authUser.UserRoles.Length);
        Assert.IsTrue(authUser.UserRoles.Any(x => x.AppId == "*" && x.RoleName == role1.RoleName));
        Assert.IsTrue(authUser.UserRoles.Any(x => x.AppId == "1" && x.RoleName == role1.RoleName));
        Assert.IsTrue(authUser.UserRoles.Any(x => x.AppId == "1" && x.RoleName == role2.RoleName));
    }
}