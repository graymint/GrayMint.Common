using System.Net;
using System.Net.Mime;
using System.Security.Authentication;
using GrayMint.Common.Client;
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
                var apiError = new ApiError(ex.ExceptionTypeName ?? nameof(ApiException), ex.Message)
                {
                    TypeFullName = ex.ExceptionTypeFullName,
                    InnerMessage = ex.InnerException?.Message
                };

                foreach (var key in ex.Data)
                {
                    if (key is string keyStr)
                        apiError.Data.TryAdd(keyStr, ex.Data[keyStr]?.ToString());
                    apiError.Data.TryAdd("InnerStatusCode", ex.StatusCode.ToString());
                }

                if (!string.IsNullOrEmpty(appExceptionOptions.Value.RootNamespace))
                    apiError.TypeFullName = apiError.TypeFullName?.Replace(nameof(GrayMint), appExceptionOptions.Value.RootNamespace);


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
                else if (ex is UnauthorizedAccessException || ex.InnerException is UnauthorizedAccessException) context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                else if (ex is AuthenticationException || ex.InnerException is AuthenticationException) context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                else if (ex.Data.Contains("HttpStatusCode")) context.Response.StatusCode = (int)ex.Data["HttpStatusCode"]!;
                else context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

                // create Api Error
                var apiError = new ApiError(ex);
                if (!string.IsNullOrEmpty(appExceptionOptions.Value.RootNamespace))
                    apiError.TypeFullName = apiError.TypeFullName?.Replace(nameof(GrayMint), appExceptionOptions.Value.RootNamespace);

                // write son
                var errorJson = apiError.ToJson();
                context.Response.ContentType = MediaTypeNames.Application.Json;
                await context.Response.WriteAsync(errorJson);

                logger.LogError(ex, "{Message}. ErrorInfo: {ErrorInfo}", ex.Message, errorJson);
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