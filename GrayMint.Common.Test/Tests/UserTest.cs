using System.Net;
using System.Net.Http.Headers;
using GrayMint.Common.Client;
using GrayMint.Common.Test.Helper;
using GrayMint.Common.Test.WebApiSample.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GrayMint.Common.Test.Tests;

[TestClass]
public class UserTest : BaseControllerTest
{
    [TestMethod]
    public async Task GetUserToken()
    {
        var user = await TestInit1.CreateUserAndAddToRole(TestInit.NewEmail(), RolePermission.SystemAdmin.RoleName);
        
        // call api buy retrieved token
        TestInit1.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, await TestInit1.UsersClient.GetAuthorizationTokenByEmailAsync(user.Email));
        await TestInit1.AppsClient.CreateAppAsync(Guid.NewGuid().ToString());
    }

    [TestMethod]
    public async Task ResetAuthUserToken()
    {
        var testInit = await TestInit.Create();
        var user = await testInit.CreateUserAndAddToRole(TestInit.NewEmail(), RolePermission.SystemAdmin.RoleName);

        // call api buy retrieved token
        testInit.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, await testInit.UsersClient.GetAuthorizationTokenByEmailAsync(user.Email));
        await testInit.AppsClient.CreateAppAsync(Guid.NewGuid().ToString()); // make sure the current token is working

        //reset token
        await testInit.UsersClient.GetAuthorizationTokenByEmailAsync(user.Email, true);
        await Task.Delay(200);
        try
        {
            await testInit.AppsClient.CreateAppAsync(Guid.NewGuid().ToString());
            Assert.Fail("Unauthorized Exception was expected.");
        }
        catch (ApiException ex)
        {
            Assert.AreEqual((int)HttpStatusCode.Unauthorized, ex.StatusCode);
        }
    }

    [TestMethod]
    public async Task ResetMyAuthToken()
    {
        var testInit = await TestInit.Create();
        var user = await testInit.CreateUserAndAddToRole(TestInit.NewEmail(), RolePermission.AppUser.RoleName);

        // call api buy retrieved token
        testInit.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, 
            await testInit.UsersClient.GetAuthorizationTokenByEmailAsync(user.Email));

        //reset token
        var newToken = await testInit.UsersClient.ResetMyTokenAsync(); // make sure the current token is working
        await Task.Delay(100);
        try
        {
            await testInit.UsersClient.ResetMyTokenAsync(); // current token should not work anymore
            Assert.Fail("Unauthorized Exception was expected.");
        }
        catch (ApiException ex)
        {
            Assert.AreEqual((int)HttpStatusCode.Unauthorized, ex.StatusCode);
        }

        //check the new token
        testInit.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, newToken);

    }

}