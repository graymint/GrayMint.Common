using System.Net;
using System.Text.Json;
using GrayMint.Common.Client;
using GrayMint.Common.Test.Helper;
using GrayMint.Common.Test.WebApiSample.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GrayMint.Common.Test.Tests;

[TestClass]
public class AccessTest
{
    [TestMethod]
    public async Task Foo()
    {
        await Task.Delay(0);
    }

    [TestMethod]
    public async Task AppCreator_access()
    {
        using var testInit = await TestInit.Create();

        // Create an AppCreator
        var appCreatorUser1 = await testInit.CreateUserAndAddToRole(TestInit.NewEmail(), Roles.SystemAdmin);

        // **** Check: accept All apps permission
        testInit.HttpClient.DefaultRequestHeaders.Authorization =
            await testInit.CreateAuthorizationHeader(appCreatorUser1.Email);
        await testInit.AppsClient.CreateAppAsync(Guid.NewGuid().ToString());

        // **** Check: refuse if caller does not have all app permission
        try
        {
            var appCreatorUser2 = await testInit.CreateUserAndAddToRole(TestInit.NewEmail(), Roles.SystemAdmin, "123");
            testInit.HttpClient.DefaultRequestHeaders.Authorization =
                await testInit.CreateAuthorizationHeader(appCreatorUser2.Email);
            await testInit.AppsClient.CreateAppAsync(Guid.NewGuid().ToString());
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
        using var testInit = await TestInit.Create();

        // Create an AppCreator
        // **** Check: accept create item by AllApps access
        var appUser = await testInit.CreateUserAndAddToRole(TestInit.NewEmail(), Roles.AppUser);
        testInit.HttpClient.DefaultRequestHeaders.Authorization = await testInit.CreateAuthorizationHeader(appUser.Email);
        await testInit.ItemsClient.CreateAsync(testInit.App.AppId, Guid.NewGuid().ToString());

        // **** Check: accept create item by the App permission
        appUser = await testInit.CreateUserAndAddToRole(TestInit.NewEmail(), Roles.AppUser, testInit.App.AppId.ToString());
        testInit.HttpClient.DefaultRequestHeaders.Authorization = await testInit.CreateAuthorizationHeader(appUser.Email);
        await testInit.ItemsClient.CreateAsync(testInit.App.AppId, Guid.NewGuid().ToString());

        // **** Check: refuse if caller does not have all the app permission
        try
        {
            appUser = await testInit.CreateUserAndAddToRole(TestInit.NewEmail(), Roles.AppUser, (testInit.App.AppId - 1).ToString());
            testInit.HttpClient.DefaultRequestHeaders.Authorization = await testInit.CreateAuthorizationHeader(appUser.Email);
            await testInit.ItemsClient.CreateAsync(testInit.App.AppId, Guid.NewGuid().ToString());
            Assert.Fail("Forbidden Exception was expected.");
        }
        catch (ApiException ex)
        {
            Assert.AreEqual((int)HttpStatusCode.Forbidden, ex.StatusCode);
        }
    }

    [TestMethod]
    public async Task AppUser_access_by_permission()
    {
        var testInit1 = await TestInit.Create();
        var testInit2 = await TestInit.Create();
        // Create an AppCreator

        // **** Check: accept create item by Create Permission
        var appUser = await testInit1.CreateUserAndAddToRole(TestInit.NewEmail(), Roles.SystemAdmin);
        testInit1.HttpClient.DefaultRequestHeaders.Authorization = await testInit1.CreateAuthorizationHeader(appUser.Email);
        await testInit1.ItemsClient.CreateByPermissionAsync(testInit1.App.AppId, Guid.NewGuid().ToString());

        // **** Check: accept create item by the App permission
        appUser = await testInit1.CreateUserAndAddToRole(TestInit.NewEmail(), Roles.AppUser, testInit1.App.AppId.ToString());
        testInit1.HttpClient.DefaultRequestHeaders.Authorization = await testInit1.CreateAuthorizationHeader(appUser.Email);
        await testInit1.ItemsClient.CreateByPermissionAsync(testInit1.App.AppId, Guid.NewGuid().ToString());

        // **** Check: refuse if caller belong to other app and does not have all the app permission
        try
        {
            appUser = await testInit1.CreateUserAndAddToRole(TestInit.NewEmail(), Roles.AppUser, testInit2.App.AppId.ToString());
            testInit1.HttpClient.DefaultRequestHeaders.Authorization = await testInit1.CreateAuthorizationHeader(appUser.Email);
            await testInit1.ItemsClient.CreateByPermissionAsync(testInit1.App.AppId, Guid.NewGuid().ToString());
            Assert.Fail("Forbidden Exception was expected.");
        }
        catch (ApiException ex)
        {
            Assert.AreEqual((int)HttpStatusCode.Forbidden, ex.StatusCode);
        }

        // **** Check: refuse if caller does not have write permission
        try
        {
            appUser = await testInit1.CreateUserAndAddToRole(TestInit.NewEmail(), Roles.AppReader, testInit1.App.AppId.ToString());
            testInit1.HttpClient.DefaultRequestHeaders.Authorization = await testInit1.CreateAuthorizationHeader(appUser.Email);
            await testInit1.ItemsClient.CreateByPermissionAsync(testInit1.App.AppId, Guid.NewGuid().ToString());
            Assert.Fail("Forbidden Exception was expected.");
        }
        catch (ApiException ex)
        {
            Assert.AreEqual((int)HttpStatusCode.Forbidden, ex.StatusCode);
        }

    }
}