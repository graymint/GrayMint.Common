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
            // a started response can't take a status any more, so the server logs the exception and aborts it
            catch (ApiException ex) when (!context.Response.HasStarted)
            {
                var apiError = ex.ToApiError();
                apiError = ApplyRootNamespace(apiError, appExceptionOptions.Value.RootNamespace);

                var errorJson = apiError.ToJson();
                context.Response.ContentType = MediaTypeNames.Application.Json;
                context.Response.StatusCode = ex.StatusCode;
                await context.Response.WriteAsync(errorJson);

                logger.Log(GetLogLevel(ex.StatusCode), ex, "{Message}. ErrorInfo: {ErrorInfo}", ex.Message, errorJson);
            }
            catch (Exception ex) when (!context.Response.HasStarted)
            {
                var statusCode = GetStatusCode(ex);
                context.Response.StatusCode = statusCode ?? (int)HttpStatusCode.BadRequest;

                // create Api Error
                var apiError = ex.ToApiError();
                apiError = ApplyRootNamespace(apiError, appExceptionOptions.Value.RootNamespace);

                // write json
                var errorJson = apiError.ToJson();
                context.Response.ContentType = MediaTypeNames.Application.Json;
                await context.Response.WriteAsync(errorJson);

                logger.Log(GetLogLevel(statusCode), ex, "{Message}. ErrorInfo: {ErrorInfo}", ex.Message, errorJson);
            }
        }

        // null for an exception the app doesn't name a status for, which answers 400
        private static int? GetStatusCode(Exception ex)
        {
            if (NotExistsException.Is(ex)) return (int)HttpStatusCode.NotFound;
            if (AlreadyExistsException.Is(ex)) return (int)HttpStatusCode.Conflict;
            if (ex is UnauthorizedAccessException || ex.InnerException is UnauthorizedAccessException)
                return (int)HttpStatusCode.Forbidden;
            if (ex is AuthenticationException || ex.InnerException is AuthenticationException)
                return (int)HttpStatusCode.Unauthorized;
            return ex.Data["HttpStatusCode"] is int statusCode ? statusCode : null;
        }

        // a client's error the app names (not found, unauthorized, a status it set) is routine; any other may be a bug
        private static LogLevel GetLogLevel(int? statusCode)
        {
            return statusCode is >= 400 and < 500 ? LogLevel.Information : LogLevel.Error;
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