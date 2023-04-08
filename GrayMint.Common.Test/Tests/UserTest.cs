using System.Net;
using GrayMint.Common.AspNetCore.SimpleUserControllers.Exceptions;
using System.Security.Claims;
using GrayMint.Common.Client;
using GrayMint.Common.Test.Api;
using GrayMint.Common.Test.Helper;
using GrayMint.Common.Test.WebApiSample.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Namotion.Reflection;
using GrayMint.Common.AspNetCore.Auth.BotAuthentication;
using static System.Formats.Asn1.AsnWriter;
using static System.Net.WebRequestMethods;
using System.Net.Http.Headers;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.Extensions.DependencyInjection;

namespace GrayMint.Common.Test.Tests;

[TestClass]
public class UserTest
{
    [TestMethod]
    public async Task ResetAuthUserToken()
    {
        var testInit = await TestInit.Create();
        await testInit.AddNewUser(Roles.SystemAdmin);
        var apiKey = await testInit.TeamClient.ResetCurrentUserApiKeyAsync();

        // call api buy retrieved token
        testInit.SetApiKey(apiKey);
        await testInit.AppsClient.CreateAppAsync(Guid.NewGuid().ToString()); // make sure the current token is working

        //reset token
        await testInit.TeamClient.ResetCurrentUserApiKeyAsync();
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
    public async Task ResetSystemBotAuthToken()
    {
        var testInit = await TestInit.Create();
        var user = await testInit.AddNewUser(Roles.SystemAdmin);

        // call api buy retrieved token
        var apiKey = await testInit.TeamClient.ResetSystemBotApiKeyAsync(user.UserId);
        testInit.SetApiKey(apiKey);
        await testInit.AppsClient.CreateAppAsync(Guid.NewGuid().ToString()); // make sure the current token is working

        //reset token
        await testInit.TeamClient.ResetSystemBotApiKeyAsync(user.UserId);
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
    public async Task ResetAppBotAuthToken()
    {
        var testInit = await TestInit.Create();
        var apiKey = await testInit.AddNewUser(Roles.AppAdmin);

        // call api buy retrieved token
        apiKey = await testInit.TeamClient.ResetAppBotApiKeyAsync(testInit.App.AppId, apiKey.UserId);
        testInit.SetApiKey(apiKey);
        await testInit.ItemsClient.CreateAsync(testInit.AppId, Guid.NewGuid().ToString()); // make sure the current token is working

        //reset token
        await testInit.TeamClient.ResetAppBotApiKeyAsync(testInit.AppId, apiKey.UserId);
        await Task.Delay(200);
        try
        {
            await testInit.ItemsClient.CreateAsync(testInit.AppId, Guid.NewGuid().ToString()); // make sure the current token is working
            Assert.Fail("Unauthorized Exception was expected.");
        }
        catch (ApiException ex)
        {
            Assert.AreEqual((int)HttpStatusCode.Unauthorized, ex.StatusCode);
        }
    }

    [TestMethod]
    public async Task ResetSystemBotAuthToken_should_not_work_for_user()
    {
        using var testInit = await TestInit.Create();

        var userRole = await testInit.TeamClient.AddSystemUserAsync(new TeamAddUserParam
        {
            Email = $"{Guid.NewGuid()}@mail.com",
            RoleId = Roles.SystemAdmin.RoleId
        });

        try
        {
            await testInit.TeamClient.ResetSystemBotApiKeyAsync(userRole.User.UserId);
            Assert.Fail("InvalidOperationException was expected.");
        }
        catch (ApiException ex)
        {
            Assert.AreEqual(nameof(InvalidOperationException), ex.ExceptionTypeName);
        }
    }

    [TestMethod]
    public async Task ResetAppBotAuthToken_should_not_work_for_user()
    {
        using var testInit = await TestInit.Create();

        var userRole = await testInit.TeamClient.AddAppUserAsync(testInit.AppId, new TeamAddUserParam
        {
            Email = $"{Guid.NewGuid()}@mail.com",
            RoleId = Roles.AppAdmin.RoleId
        });

        try
        {
            await testInit.TeamClient.ResetAppBotApiKeyAsync(testInit.AppId, userRole.User.UserId);
            Assert.Fail("InvalidOperationException was expected.");
        }
        catch (ApiException ex)
        {
            Assert.AreEqual(nameof(InvalidOperationException), ex.ExceptionTypeName);
        }
    }

    private static async Task<AuthenticationHeaderValue> CreateUnregisteredUserAuthorization(IServiceScope scope, string email, Claim[]? claims = null)
    {
        var claimsIdentity = new ClaimsIdentity();
        claimsIdentity.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, email));
        claimsIdentity.AddClaim(new Claim(JwtRegisteredClaimNames.Email, email));
        claimsIdentity.AddClaim(new Claim("test_authenticated", "1"));
        if (claims != null)
            claimsIdentity.AddClaims(claims);

        var authenticationTokenBuilder = scope.ServiceProvider.GetRequiredService<BotAuthenticationTokenBuilder>();
        return await authenticationTokenBuilder.CreateAuthenticationHeader(claimsIdentity);
    }

    [TestMethod]
    public async Task RegisterCurrentUser()
    {
        var testInit = await TestInit.Create();
        var userEmail = TestInit.NewEmail();

        // ------------
        // Check: New user should not exist if not he hasn't registered yet
        // ------------
        testInit.HttpClient.DefaultRequestHeaders.Authorization = await CreateUnregisteredUserAuthorization(testInit.Scope, userEmail);
        try
        {
            await testInit.TeamClient.GetCurrentUserAppsAsync();
            Assert.Fail("User should not exist!");
        }
        catch (ApiException ex)
        {
            Assert.AreEqual(nameof(UnregisteredUser), ex.ExceptionTypeName);
        }

        // ------------
        // Check: Register current user
        // ------------
        await testInit.TeamClient.RegisterCurrentUserAsync();
        var user = await testInit.TeamClient.GetCurrentUserAsync();
        Assert.AreEqual(userEmail, user.Email);

        // Get Project Get
        var apps = await testInit.TeamClient.GetCurrentUserAppsAsync();
        Assert.AreEqual(0, apps.Count);
    }
}