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
public class AwsCognitoTest
{
    public async Task<string> GetCredentialsAsync(string email, string password)
    {
        using var testInit = await TestInit.Create();
        var cognitoArn = Arn.Parse(testInit.CognitoAuthenticationOptions.CognitoArn);
        var awsRegion = RegionEndpoint.GetBySystemName(cognitoArn.Region);
        var provider = new AmazonCognitoIdentityProviderClient(new Amazon.Runtime.AnonymousAWSCredentials(), awsRegion);
        var userPool = new CognitoUserPool(cognitoArn.Resource, testInit.CognitoAuthenticationOptions.CognitoClientId, provider);
        var user = new CognitoUser(email, testInit.CognitoAuthenticationOptions.CognitoClientId, userPool, provider);
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
        using var testInit = await TestInit.Create();

        // add user to appCreator role
        try
        {
            await testInit.CreateUserAndAddToRole("unit-tester@local", Roles.SystemAdmin);
        }
        catch (Exception ex) when (ex.InnerException != null && ex.InnerException!.Message.Contains("Cannot insert duplicate key in object"))
        {
        }

        var idToken = await GetCredentialsAsync("unit-tester", "Password1@");
        testInit.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, idToken);

        await testInit.AppsClient.ListAsync();
    }
}