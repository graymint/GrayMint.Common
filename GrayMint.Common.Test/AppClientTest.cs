using System.Net;
using System.Net.Http.Headers;
using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.Extensions.CognitoAuthentication;
using GrayMint.Common.Client;
using GrayMint.Common.Test.Helper;
using GrayMint.Common.Test.WebApiSample;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GrayMint.Common.Test;

[TestClass]
public class AppCreatorAccessTest : BaseControllerTest
{

    [TestMethod]
    public async Task AppCreator_access()
    {
        // Create an AppCreator
        var appCreatorUser1 = await TestInit1.CreateUserAndAddToRole(TestInit.NewEmail(), Roles.AppCreator);

        // **** Check: accept All apps permission
        TestInit1.HttpClient.DefaultRequestHeaders.Authorization = await TestInit1.CreateAuthorizationHeader(appCreatorUser1.Email);
        await TestInit1.AppsClient.CreateAppAsync(Guid.NewGuid().ToString());

        // **** Check: refuse if caller does not have all app permission
        try
        {
            var appCreatorUser2 = await TestInit1.CreateUserAndAddToRole(TestInit.NewEmail(), Roles.AppCreator, "123");
            TestInit1.HttpClient.DefaultRequestHeaders.Authorization = await TestInit1.CreateAuthorizationHeader(appCreatorUser2.Email);
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
        TestInit1.HttpClient.DefaultRequestHeaders.Authorization = await TestInit1.CreateAuthorizationHeader(appUser.Email);
        await TestInit1.ItemsClient.CreateAsync(TestInit1.App.AppId, Guid.NewGuid().ToString());

        // **** Check: accept create item by the App permission
        appUser = await TestInit1.CreateUserAndAddToRole(TestInit.NewEmail(), Roles.AppUser, TestInit1.App.AppId.ToString());
        TestInit1.HttpClient.DefaultRequestHeaders.Authorization = await TestInit1.CreateAuthorizationHeader(appUser.Email);
        await TestInit1.ItemsClient.CreateAsync(TestInit1.App.AppId, Guid.NewGuid().ToString());

        // **** Check: refuse if caller does not have all the app permission
        try
        {
            appUser = await TestInit1.CreateUserAndAddToRole(TestInit.NewEmail(), Roles.AppUser, (TestInit1.App.AppId - 1).ToString());
            TestInit1.HttpClient.DefaultRequestHeaders.Authorization = await TestInit1.CreateAuthorizationHeader(appUser.Email);
            await TestInit1.ItemsClient.CreateAsync(TestInit1.App.AppId, Guid.NewGuid().ToString());
            Assert.Fail("Forbidden Exception was expected.");
        }
        catch (ApiException ex)
        {
            Assert.AreEqual((int)HttpStatusCode.Forbidden, ex.StatusCode);
        }
    }

    public async Task<string> GetCredsAsync()
    {
        var provider = new AmazonCognitoIdentityProviderClient(new Amazon.Runtime.AnonymousAWSCredentials(), RegionEndpoint.USEast2);
        var userPool = new CognitoUserPool("us-east-2_6Ojx0unPh", TestInit1.CognitoAuthenticationOptions.CognitoClientId, provider);
        var user = new CognitoUser("madnik", TestInit1.CognitoAuthenticationOptions.CognitoClientId, userPool, provider);
        var authRequest = new InitiateSrpAuthRequest()
        {
            Password = "Password1@"
        };

        var authResponse = await user.StartWithSrpAuthAsync(authRequest).ConfigureAwait(false);
        var accessToken = authResponse.AuthenticationResult.AccessToken;
        return accessToken;
    }

    [TestMethod]
    public async Task CognitoTest()
    {
        await GetCredsAsync();

        var token = "eyJraWQiOiI3OXdGZTBEMU4xZVluS3EyNURaQk5YclBYWm5pQlZOYU8ybEdUQ3NwelwvUT0iLCJhbGciOiJSUzI1NiJ9.eyJzdWIiOiIyNzNmZmYyMC0wMzQ3LTQwYjItOWI0Mi02NDVjNWE1MDY3ODkiLCJjb2duaXRvOmdyb3VwcyI6WyJQYXltZW50X0FkbWluIiwiTXlHcm91cCIsIkZvbzEwMCJdLCJpc3MiOiJodHRwczpcL1wvY29nbml0by1pZHAudXMtZWFzdC0yLmFtYXpvbmF3cy5jb21cL3VzLWVhc3QtMl82T2p4MHVuUGgiLCJ2ZXJzaW9uIjoyLCJjbGllbnRfaWQiOiIyaDlmaml1YmVsbGNub2FwdWhtajdvcXNwYyIsImV2ZW50X2lkIjoiM2ZhMDUyZjUtYTU4MS00Y2NlLTg4NjAtZGQwNzY0ZjQwODNjIiwidG9rZW5fdXNlIjoiYWNjZXNzIiwic2NvcGUiOiJvcGVuaWQgaHR0cHM6XC9cL3dhbGxldC5jb21cL1Njb3BlMSBlbWFpbCIsImF1dGhfdGltZSI6MTY2NDY5MTg0MCwiZXhwIjoxNjY0Njk1NDQwLCJpYXQiOjE2NjQ2OTE4NDAsImp0aSI6Ijg0ZGJjY2JkLWY5MDAtNDVmMS1iYzVkLWE1OGFmNzM0OGUyYSIsInVzZXJuYW1lIjoibWFkbmlrIn0.UpuxxFOkhS43iICIfPXPI47LOue0eLRjngmJ3B7mV-QdIsS0YNKCB8v1HFJOoGL2dvmp21ctDbiWOD65KArcK1UA18g8OyRXHwCuaelVJ5BBlJunI342keezXrf9WYyWSM6yarMck7M5EPh6xOZFf5_QNrPu17-xm-WIJB2kWIjluHguFbtCS1IxO06zyxnvPTwidGn9COuX8o9Y3j1UWGGjuJyZm9ueii0F4oe4En7YJ6abczYmLxCoK8kLyxRw7Czj3PMfUM3MpWz0nXkLwdKdoFsonO-T10stbrc5zOi90keTikPNjnYbOw2IB-OkgNvZVDLwWgTJP31yINGIFQ";
        TestInit1.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token);
        await TestInit1.AppsClient.ListAsync();
    }

}
