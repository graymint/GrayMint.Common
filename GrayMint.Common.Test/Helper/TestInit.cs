using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using GrayMint.Common.AspNetCore.Auth.CognitoAuthentication;
using GrayMint.Common.AspNetCore.SimpleRoleAuthorization;
using GrayMint.Common.Test.Api;
using GrayMint.Common.Test.WebApiSample;
using Microsoft.Extensions.Options;
using GrayMint.Common.AspNetCore.Auth.BotAuthentication;

namespace GrayMint.Common.Test.Helper;

public class TestInit : IDisposable
{
    public WebApplicationFactory<Program> WebApp { get; }
    public HttpClient HttpClient { get; set; }
    public IServiceScope Scope { get; }
    public App App { get; private set; } = default!;
    public int AppId => App.AppId;
    public CognitoAuthenticationOptions CognitoAuthenticationOptions => WebApp.Services.GetRequiredService<IOptions<CognitoAuthenticationOptions>>().Value;
    public AppsClient AppsClient => new(HttpClient);
    public UsersClient UsersClient => new(HttpClient);
    public ItemsClient ItemsClient => new(HttpClient);
    public TeamClient TeamClient => new(HttpClient);
    public ApiKeyResult SystemAdminApiKey { get; private set; } = default!;


    private TestInit(Dictionary<string, string?> appSettings, string environment)
    {
        // Application
        WebApp = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                foreach (var appSetting in appSettings)
                    builder.UseSetting(appSetting.Key, appSetting.Value);

                builder.UseEnvironment(environment);
                builder.ConfigureServices(services =>
                {
                    services.AddScoped<IBotAuthenticationProvider, TestBotAuthenticationProvider>();
                });
            });

        // Client
        HttpClient = WebApp.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Create System user
        Scope = WebApp.Services.CreateScope();
    }

    private async Task Init()
    {
        SystemAdminApiKey = await TeamClient.CreateSystemApiKeyAsync();
        HttpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(SystemAdminApiKey.Authorization);
        App = await AppsClient.CreateAppAsync(Guid.NewGuid().ToString());
    }

    public async Task<ApiKeyResult> AddNewUser(SimpleRole simpleRole, bool setAsCurrent = true)
    {
        var oldAuthorization = HttpClient.DefaultRequestHeaders.Authorization;
        HttpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(SystemAdminApiKey.Authorization);

        var apiKey = simpleRole.IsSystem
            ? await TeamClient.CreateSystemBotAsync(new TeamAddBotParam { Name = Guid.NewGuid().ToString(), RoleId = simpleRole.RoleId })
            : await TeamClient.CreateAppBotAsync(App.AppId, new TeamAddBotParam { Name = Guid.NewGuid().ToString(), RoleId = simpleRole.RoleId });

        HttpClient.DefaultRequestHeaders.Authorization = setAsCurrent
            ? AuthenticationHeaderValue.Parse(apiKey.Authorization) : oldAuthorization;

        return apiKey;
    }

    public static async Task<TestInit> Create(Dictionary<string, string?>? appSettings = null, string environment = "Development")
    {
        appSettings ??= new Dictionary<string, string?>();
        var testInit = new TestInit(appSettings, environment);
        await testInit.Init();
        return testInit;
    }

    public void SetApiKey(ApiKeyResult apiKey)
    {
        HttpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(apiKey.Authorization);
    }


    public static string NewEmail()
    {
        return $"{Guid.NewGuid()}@local";
    }

    public void Dispose()
    {
        Scope.Dispose();
        HttpClient.Dispose();
        WebApp.Dispose();
    }
}