using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GrayMint.Common.AspNetCore.Auth.CognitoAuthentication;

public static class CognitoAuthenticationExtension
{
    public static AuthenticationBuilder AddCognitoAuthentication(this AuthenticationBuilder authenticationBuilder, IConfiguration configuration)
    {
        var cognitoOptions = configuration.Get<CognitoAuthenticationOptions>();
        cognitoOptions.Validate();

        authenticationBuilder
            .AddJwtBearer(CognitoAuthenticationDefaults.AuthenticationScheme, options =>
            {
                var cognitoArn = new AwsArn(cognitoOptions.CognitoArn);
                options.RequireHttpsMetadata = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = cognitoOptions.CognitoArn,
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateLifetime = false, //todo
                    ValidateAudience = false,
                };
                options.MetadataAddress = $"https://{cognitoArn.Service}.{cognitoArn.Region}.amazonaws.com/{cognitoArn.ResourceId}/.well-known/openid-configuration";

                options.SaveToken = true;


                options.Events = new JwtBearerEvents()
                {
                    OnTokenValidated = async context =>
                    {
                        await using var scope = context.HttpContext.RequestServices.CreateAsyncScope();
                        var tokenValidator = scope.ServiceProvider.GetRequiredService<CognitoTokenValidator>();
                        await tokenValidator.Validate(context);
                    }
                };
            });

        authenticationBuilder.Services.AddSingleton<CognitoTokenValidator>();
        authenticationBuilder.Services.Configure<CognitoAuthenticationOptions>(configuration);
        return authenticationBuilder;
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    private class OpenIdUserInfo
    {
        [JsonPropertyName("sub")]

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        // ReSharper disable once UnusedMember.Local
        public string Sub { get; init; } = default!;

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        [JsonPropertyName("email_verified")]
        public string? EmailVerified { get; init; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        [JsonPropertyName("email")]
        public string? Email { get; init; }

        // ReSharper disable once UnusedMember.Local
        [JsonPropertyName("username")]
        public string? Name { get; init; }
    }

    private class CognitoTokenValidator
    {
        private readonly HttpClient _httpClient;
        private readonly IOptions<CognitoAuthenticationOptions> _cognitoOptions;
        private readonly IMemoryCache _memoryCache;

        public CognitoTokenValidator(HttpClient httpClient,
            IOptions<CognitoAuthenticationOptions> cognitoOptions,
            IMemoryCache memoryCache)
        {
            _httpClient = httpClient;
            _cognitoOptions = cognitoOptions;
            _memoryCache = memoryCache;
        }

        private async Task<OpenIdUserInfo> GetUserInfoFromAccessToken(TokenValidatedContext context)
        {
            var jwtSecurityToken = (JwtSecurityToken)context.SecurityToken;
            var accessToken = jwtSecurityToken.RawData;

            // get from cache
            var accessTokenHash = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(accessToken));
            var cacheKey = "OpenIdUserInfo/" + Convert.ToBase64String(accessTokenHash);
            if (_memoryCache.TryGetValue<OpenIdUserInfo>(cacheKey, out var userInfo))
                return userInfo;

            // get from authority
            if (context.Options.ConfigurationManager == null)
                throw new UnauthorizedAccessException("ConfigurationManager is not set.");
            var configuration = await context.Options.ConfigurationManager.GetConfigurationAsync(CancellationToken.None);
            var tokenUse = jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == "token_use")?.Value
                           ?? throw new UnauthorizedAccessException("Could not find token_use.");

            // get userInfo message from id token
            OpenIdUserInfo openIdUserInfo;
            if (tokenUse == "id")
            {
                openIdUserInfo = new OpenIdUserInfo()
                {
                    Sub = jwtSecurityToken.Claims.Single(x => x.Type == "sub").Value,
                    Email = jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value,
                    EmailVerified = jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == "email_verified")?.Value,
                };
            }
            else
            {
                // get userInfo message access token
                if (!jwtSecurityToken.Claims.Any(x => x.Type == "scope" && x.Value.Split(' ').Contains("openid")))
                    throw new UnauthorizedAccessException("openid scope was expected.");

                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, configuration.UserInfoEndpoint);
                httpRequestMessage.Headers.Authorization =
                    new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, accessToken);
                var httpResponseMessage = await _httpClient.SendAsync(httpRequestMessage);
                httpResponseMessage.EnsureSuccessStatusCode();
                var json = await httpResponseMessage.Content.ReadAsStringAsync();
                openIdUserInfo = EzUtil.JsonDeserialize<OpenIdUserInfo>(json);
            }

            _memoryCache.Set(cacheKey, openIdUserInfo);
            return openIdUserInfo;
        }

        public async Task Validate(TokenValidatedContext context)
        {
            if (context.Principal == null)
            {
                context.Fail("Principal does not exist.");
                return;
            }

            // validate audience or client
            var jwtSecurityToken = (JwtSecurityToken)context.SecurityToken;
            var tokenUse = jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == "token_use")?.Value
                           ?? throw new UnauthorizedAccessException("Could not find token_use.");
            if (tokenUse != "access" && tokenUse != "id") throw new UnauthorizedAccessException("Unknown token_use.");

            // aud for id token
            if (tokenUse == "id" && !context.Principal.HasClaim(x => x.Type == "aud" && x.Value == _cognitoOptions.Value.CognitoClientId))
            {
                context.Fail("client_id does not match");
                return;
            }

            // aud for id token
            if (tokenUse == "access" && !context.Principal.HasClaim(x => x.Type == "client_id" && x.Value == _cognitoOptions.Value.CognitoClientId))
            {
                context.Fail("client_id does not match");
                return;
            }

            // get user_info from authority by AccessToken
            var userInfo = await GetUserInfoFromAccessToken(context);
            if (userInfo.EmailVerified != "true")
            {
                context.Fail("User's email is not verified.");
                return;
            }

            // add claims
            var claimsIdentity = new ClaimsIdentity();

            // add email claim
            if (!context.Principal.HasClaim(x => x.Type == ClaimTypes.Email) && !string.IsNullOrEmpty(userInfo.Email))
                claimsIdentity.AddClaim(new Claim(ClaimTypes.Email, userInfo.Email));

            // Convert cognito roles to standard roles
            foreach (var claim in context.Principal.Claims.Where(x => x.Type == "cognito:groups"))
                claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, _cognitoOptions.Value.CognitoRolePrefix + claim.Value));

            context.Principal?.AddIdentity(claimsIdentity);
        }
    }

}