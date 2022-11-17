using System.Collections;
using System.Net;
using System.Net.Mime;
using System.Text.Json;
using GrayMint.Common.Exceptions;
using Microsoft.Extensions.Options;

namespace GrayMint.Common.AspNetCore;

public static class AppExceptionExtension
{
    public class AppExceptionOptions
    {
        public string? RootNamespace { get; set; }
    }

    public class CustomExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly AppExceptionOptions _appExceptionOptions;

        public CustomExceptionMiddleware(RequestDelegate next, IOptions<AppExceptionOptions> appExceptionOptions)
        {
            _next = next;
            _appExceptionOptions = appExceptionOptions.Value;
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
                if (!string.IsNullOrEmpty(_appExceptionOptions.RootNamespace))
                    typeFullName = typeFullName?.Replace(nameof(GrayMint), _appExceptionOptions.RootNamespace);

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

    public static IApplicationBuilder UseAppExceptionHandler(this IApplicationBuilder app, AppExceptionOptions? appExceptionOptions = null)
    {
        appExceptionOptions ??= new AppExceptionOptions();
        app.UseMiddleware<CustomExceptionMiddleware>(Options.Create(appExceptionOptions));
        return app;
    }
}