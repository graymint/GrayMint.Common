using System.Collections;
using System.Net;
using System.Net.Mime;
using System.Text.Json;
using GrayMint.Common.Exceptions;
using Microsoft.Extensions.Options;

namespace GrayMint.Common.AspNetCore;

public static class GrayMintExceptionHandlerExtension
{
    public class GrayMintExceptionOptions
    {
        public string? RootNamespace { get; set; }
    }

    public class GrayMintExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly GrayMintExceptionOptions _grayMintExceptionOptions;

        public GrayMintExceptionMiddleware(RequestDelegate next, IOptions<GrayMintExceptionOptions> appExceptionOptions)
        {
            _next = next;
            _grayMintExceptionOptions = appExceptionOptions.Value;
        }

        private static Type GetExceptionType(Exception ex)
        {
            if (AlreadyExistsException.Is(ex)) return typeof(AlreadyExistsException);
            if (NotExistsException.Is(ex)) return typeof(NotExistsException);
            return ex.GetType();
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
                else if (ex is UnauthorizedAccessException) context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                else context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

                // create typeFullName
                var typeFullName = GetExceptionType(ex).FullName;
                if (!string.IsNullOrEmpty(_grayMintExceptionOptions.RootNamespace))
                    typeFullName = typeFullName?.Replace(nameof(GrayMint), _grayMintExceptionOptions.RootNamespace);

                // set optional information
                context.Response.ContentType = MediaTypeNames.Application.Json;
                var error = new
                {
                    Data = new Dictionary<string, string?>(),
                    TypeName = GetExceptionType(ex).Name,
                    TypeFullName = typeFullName,
                    ex.Message
                };



                foreach (DictionaryEntry item in ex.Data)
                {
                    var key = item.Key.ToString();
                    if (key != null)
                        error.Data.Add(key, item.Value?.ToString());
                }
                await context.Response.WriteAsync(JsonSerializer.Serialize(error));
            }
        }
    }

    public static IApplicationBuilder UseGrayMintExceptionHandler(this IApplicationBuilder app, GrayMintExceptionOptions? appExceptionOptions = null)
    {
        appExceptionOptions ??= new GrayMintExceptionOptions();
        app.UseMiddleware<GrayMintExceptionMiddleware>(Options.Create(appExceptionOptions));
        return app;
    }
}