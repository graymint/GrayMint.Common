using System.Net;
using System.Net.Mime;
using System.Security.Authentication;
using GrayMint.Common.Exceptions;
using Microsoft.Extensions.Options;

namespace GrayMint.Common.AspNetCore;

public static class GrayMintExceptionHandlerExtension
{
    public class GrayMintExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly GrayMintExceptionHandlerOptions _grayMintExceptionOptions;
        private readonly ILogger<GrayMintExceptionMiddleware> _logger;

        public GrayMintExceptionMiddleware(RequestDelegate next, IOptions<GrayMintExceptionHandlerOptions> appExceptionOptions, ILogger<GrayMintExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            _grayMintExceptionOptions = appExceptionOptions.Value;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next.Invoke(context);
            }
            catch (Exception ex)
            {
                // set correct https status code depends on exception
                if (NotExistsException.Is(ex)) context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                else if (AlreadyExistsException.Is(ex)) context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                else if (ex is UnauthorizedAccessException || ex.InnerException is UnauthorizedAccessException) context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                else if (ex is AuthenticationException || ex.InnerException is AuthenticationException) context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                else if (ex.Data.Contains("HttpStatusCode")) context.Response.StatusCode = (int)ex.Data["HttpStatusCode"]!;
                else context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

                // create portable exception
                var portableException = new PortableException(ex);
                if (!string.IsNullOrEmpty(_grayMintExceptionOptions.RootNamespace))
                    portableException.TypeFullName = portableException.TypeFullName?.Replace(nameof(GrayMint), _grayMintExceptionOptions.RootNamespace);

                // write son
                var errorJson = portableException.ToJson();
                context.Response.ContentType = MediaTypeNames.Application.Json;
                await context.Response.WriteAsync(errorJson);

                _logger.LogError(ex, "{Message}. ErrorInfo: {ErrorInfo}", ex.Message, errorJson);
            }
        }
    }

    public static IApplicationBuilder UseGrayMintExceptionHandler(this IApplicationBuilder app, GrayMintExceptionHandlerOptions? appExceptionOptions = null)
    {
        appExceptionOptions ??= new GrayMintExceptionHandlerOptions();
        app.UseMiddleware<GrayMintExceptionMiddleware>(Options.Create(appExceptionOptions));
        return app;
    }
}