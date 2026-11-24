using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace GrayMint.Common.Acme;

public static class AcmeChallengeMiddleware
{
    // RFC 8555 makes a token of base64url characters, at least 128 bits of them; Let's Encrypt's have 43
    private const int MaxTokenLength = 256;

    /// <summary>
    /// Answers ACME's HTTP-01 challenges, <c>GET /.well-known/acme-challenge/token</c>, on every listener: the authority
    /// reads port 80, and a redirect from there takes it to HTTPS. Any other request goes on to the rest of the pipeline,
    /// so put this first, before an HTTPS redirect.
    /// </summary>
    public static IApplicationBuilder UseAcmeChallenge(this IApplicationBuilder app,
        AcmeKeyAuthorizationProvider keyAuthorizationProvider)
    {
        return app.Use(async (context, next) =>
        {
            if (!context.Request.Path.StartsWithSegments("/.well-known/acme-challenge", StringComparison.Ordinal,
                    out var remaining))
            {
                await next(context);
                return;
            }

            var token = remaining.Value?.TrimStart('/');
            string? keyAuthorization = null;
            if ((HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method)) &&
                IsToken(token) && !string.IsNullOrEmpty(context.Request.Host.Host))
            {
                keyAuthorization = await keyAuthorizationProvider(
                    context.Request.Host.Host.TrimEnd('.').ToLowerInvariant(), token, context.RequestAborted);
            }

            if (keyAuthorization == null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync(keyAuthorization, context.RequestAborted);
        });
    }

    internal static bool IsToken([NotNullWhen(true)] string? token)
    {
        return !string.IsNullOrEmpty(token) && token.Length <= MaxTokenLength &&
               token.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
    }
}
