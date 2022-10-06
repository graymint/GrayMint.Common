using System.Net;
using GrayMint.Common.Client;
using GrayMint.Common.Test.Helper;
using GrayMint.Common.Test.WebApiSample;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GrayMint.Common.Test.Tests;

[TestClass]
public class AccessTest : BaseControllerTest
{

    [TestMethod]
    public async Task AppCreator_access()
    {
        // Create an AppCreator
        var appCreatorUser1 = await TestInit1.CreateUserAndAddToRole(TestInit.NewEmail(), Roles.AppCreator);

        // **** Check: accept All apps permission
        TestInit1.HttpClient.DefaultRequestHeaders.Authorization =
            await TestInit1.CreateAuthorizationHeader(appCreatorUser1.Email);
        await TestInit1.AppsClient.CreateAppAsync(Guid.NewGuid().ToString());

        // **** Check: refuse if caller does not have all app permission
        try
        {
            var appCreatorUser2 = await TestInit1.CreateUserAndAddToRole(TestInit.NewEmail(), Roles.AppCreator, "123");
            TestInit1.HttpClient.DefaultRequestHeaders.Authorization =
                await TestInit1.CreateAuthorizationHeader(appCreatorUser2.Email);
            await TestInit1.AppsClient.CreateAppAsync(Guid.NewGuid().ToString());
            Assert.Fail("Forbidden Exception was expected.");
        }
        catch (ApiException ex)
        {
            Assert.AreEqual((int)HttpStatusCode.Forbidden, ex.StatusCode);
        }
    }


    [TestMethod]
    public async Task AppUser_access()
    {
        // Create an AppCreator

        // **** Check: accept create item by AllApps access
        var appUser = await TestInit1.CreateUserAndAddToRole(TestInit.NewEmail(), Roles.AppUser);
        TestInit1.HttpClient.DefaultRequestHeaders.Authorization =
            await TestInit1.CreateAuthorizationHeader(appUser.Email);
        await TestInit1.ItemsClient.CreateAsync(TestInit1.App.AppId, Guid.NewGuid().ToString());

        // **** Check: accept create item by the App permission
        appUser = await TestInit1.CreateUserAndAddToRole(TestInit.NewEmail(), Roles.AppUser,
            TestInit1.App.AppId.ToString());
        TestInit1.HttpClient.DefaultRequestHeaders.Authorization =
            await TestInit1.CreateAuthorizationHeader(appUser.Email);
        await TestInit1.ItemsClient.CreateAsync(TestInit1.App.AppId, Guid.NewGuid().ToString());

        // **** Check: refuse if caller does not have all the app permission
        try
        {
            appUser = await TestInit1.CreateUserAndAddToRole(TestInit.NewEmail(), Roles.AppUser,
                (TestInit1.App.AppId - 1).ToString());
            TestInit1.HttpClient.DefaultRequestHeaders.Authorization =
                await TestInit1.CreateAuthorizationHeader(appUser.Email);
            await TestInit1.ItemsClient.CreateAsync(TestInit1.App.AppId, Guid.NewGuid().ToString());
            Assert.Fail("Forbidden Exception was expected.");
        }
        catch (ApiException ex)
        {
            Assert.AreEqual((int)HttpStatusCode.Forbidden, ex.StatusCode);
        }
    }
}