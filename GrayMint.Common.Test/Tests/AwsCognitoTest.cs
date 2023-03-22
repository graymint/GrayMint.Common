using System.Net.Http.Headers;
using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.Extensions.CognitoAuthentication;
using GrayMint.Common.Test.Helper;
using GrayMint.Common.Test.WebApiSample.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GrayMint.Common.Test.Tests;

[TestClass]
public class AwsCognitoTest : BaseControllerTest
{
    public async Task<string> GetCredentialsAsync(string email, string password)
    {
        var cognitoArn = Arn.Parse(TestInit1.CognitoAuthenticationOptions.CognitoArn);
        var awsRegion = RegionEndpoint.GetBySystemName(cognitoArn.Region);
        var provider = new AmazonCognitoIdentityProviderClient(new Amazon.Runtime.AnonymousAWSCredentials(), awsRegion);
        var userPool = new CognitoUserPool(cognitoArn.Resource, TestInit1.CognitoAuthenticationOptions.CognitoClientId, provider);
        var user = new CognitoUser(email, TestInit1.CognitoAuthenticationOptions.CognitoClientId, userPool, provider);
        var authRequest = new InitiateSrpAuthRequest()
        {
            Password = password
        };

        var authResponse = await user.StartWithSrpAuthAsync(authRequest).ConfigureAwait(false);
        var accessToken = authResponse.AuthenticationResult.IdToken;
        return accessToken;
    }

    [TestMethod]
    public async Task CognitoTest()
    {
        // add user to appCreator role
        try
        {
            await TestInit1.CreateUserAndAddToRole("unit-tester@local", RolePermission.SystemAdmin.RoleName);
        }
        catch (Exception ex) when (ex.InnerException != null && ex.InnerException!.Message.Contains("Cannot insert duplicate key in object"))
        {
        }

        var idToken = await GetCredentialsAsync("unit-tester", "Password1@");
        TestInit1.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, idToken);

        await TestInit1.AppsClient.ListAsync();
    }
}