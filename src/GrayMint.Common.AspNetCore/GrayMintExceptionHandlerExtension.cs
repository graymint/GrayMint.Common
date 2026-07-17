using System.Net;
using System.Net.Mime;
using System.Security.Authentication;
using GrayMint.Common.ApiClients;
using GrayMint.Common.Exceptions;
using Microsoft.Extensions.Options;

namespace GrayMint.Common.AspNetCore;

public static class GrayMintExceptionHandlerExtension
{
    public class GrayMintExceptionMiddleware(
        RequestDelegate next,
        IOptions<GrayMintExceptionHandlerOptions> appExceptionOptions,
        ILogger<GrayMintExceptionMiddleware> logger)
    {
        public async Task Invoke(HttpContext context)
        {
            try
            {
                await next.Invoke(context);
            }
            catch (ApiException ex)
            {
                var apiError = ex.ToApiError();
                apiError = ApplyRootNamespace(apiError, appExceptionOptions.Value.RootNamespace);

                var errorJson = apiError.ToJson();
                context.Response.ContentType = MediaTypeNames.Application.Json;
                context.Response.StatusCode = ex.StatusCode;
                await context.Response.WriteAsync(errorJson);

                logger.LogError(ex, "{Message}. ErrorInfo: {ErrorInfo}", ex.Message, errorJson);
            }
            catch (Exception ex)
            {
                // set correct https status code depends on exception
                if (NotExistsException.Is(ex)) context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                else if (AlreadyExistsException.Is(ex)) context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                else if (ex is UnauthorizedAccessException || ex.InnerException is UnauthorizedAccessException)
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                else if (ex is AuthenticationException || ex.InnerException is AuthenticationException)
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                else if (ex.Data.Contains("HttpStatusCode"))
                    context.Response.StatusCode = (int)ex.Data["HttpStatusCode"]!;
                else context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

                // create Api Error
                var apiError = ex.ToApiError();
                apiError = ApplyRootNamespace(apiError, appExceptionOptions.Value.RootNamespace);

                // write json
                var errorJson = apiError.ToJson();
                context.Response.ContentType = MediaTypeNames.Application.Json;
                await context.Response.WriteAsync(errorJson);

                logger.LogError(ex, "{Message}. ErrorInfo: {ErrorInfo}", ex.Message, errorJson);
            }
        }

        // ApiError properties are init-only, so the RootNamespace rewrite produces a new instance.
        private static ApiError ApplyRootNamespace(ApiError apiError, string? rootNamespace)
        {
            if (string.IsNullOrEmpty(rootNamespace))
                return apiError;

            return new ApiError
            {
                TypeName = apiError.TypeName,
                TypeFullName = apiError.TypeFullName?.Replace(nameof(GrayMint), rootNamespace),
                Message = apiError.Message,
                InnerMessage = apiError.InnerMessage,
                Data = apiError.Data
            };
        }
    }

    public static IApplicationBuilder UseGrayMintExceptionHandler(this IApplicationBuilder app,
        GrayMintExceptionHandlerOptions? appExceptionOptions = null)
    {
        appExceptionOptions ??= new GrayMintExceptionHandlerOptions();
        app.UseMiddleware<GrayMintExceptionMiddleware>(Options.Create(appExceptionOptions));
        return app;
    }
}