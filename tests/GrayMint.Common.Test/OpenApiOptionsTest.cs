using GrayMint.Common.OpenApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace GrayMint.Common.Test;

[TestClass]
public class OpenApiOptionsTest : TestBase
{
    [TestMethod]
    public async Task An_app_takes_only_its_own_open_api_options()
    {
        // another app of the process gave its options, as tests running side by side do
        WebApplication.CreateBuilder().Services.AddGrayMintOpenApi(new AddOpenApiOptions());

        await using var app = WebApplication.CreateBuilder().Build();
        var error = Assert.ThrowsExactly<InvalidOperationException>(() => app.UseGrayMintOpenApi());
        Assert.Contains("AddGrayMintOpenApi", error.Message);
    }
}
