using System.Collections;
using System.Net;
using System.Net.Mime;
using System.Text.Json;
using GrayMint.Common.Exceptions;

namespace GrayMint.Common.AspNetCore;

public static class AppExceptionExtension
{
    public class CustomExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public CustomExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
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

                // set optional information
                context.Response.ContentType = MediaTypeNames.Application.Json;
                var error = new
                {
                    Data = new Dictionary<string, string?>(),
                    TypeName = GetExceptionType(ex).Name,
                    TypeFullName = GetExceptionType(ex).FullName,
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

    public static IApplicationBuilder UseAppExceptionHandler(this IApplicationBuilder app)
    {
        app.UseMiddleware<CustomExceptionMiddleware>();
        return app;
    }
}