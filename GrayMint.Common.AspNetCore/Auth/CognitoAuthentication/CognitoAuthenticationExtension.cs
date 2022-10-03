using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
                options.RequireHttpsMetadata = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = cognitoOptions.CognitoIssuer,
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateLifetime = false, //todo
                    ValidateAudience = false,
                };
                options.MetadataAddress = $"{cognitoOptions.CognitoIssuer}/.well-known/openid-configuration";
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

    private class OpenIdUserInfo
    {
        [JsonPropertyName("sub")]
        public string Sub { get; set; } = default!;

        [JsonPropertyName("email_verified")]
        public string? EmailVerified { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }
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

            // get userInfo message
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, configuration.UserInfoEndpoint);
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, accessToken);
            var httpResponseMessage = await _httpClient.SendAsync(httpRequestMessage);
            httpResponseMessage.EnsureSuccessStatusCode();
            var json = await httpResponseMessage.Content.ReadAsStringAsync();
            var openIdUserInfo = EzUtil.JsonDeserialize<OpenIdUserInfo>(json);
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

            if (!context.Principal.HasClaim(x => x.Type == "client_id" && x.Value == _cognitoOptions.Value.CognitoClientId))
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