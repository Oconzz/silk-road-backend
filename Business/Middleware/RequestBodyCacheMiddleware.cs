using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Middleware
{
    public class RequestBodyCacheMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestBodyCacheMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            context.Request.EnableBuffering();

            using (var reader = new StreamReader(
                context.Request.Body,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: -1,
                leaveOpen: true))
            {
                var body = await reader.ReadToEndAsync();
                context.Items["RequestBody"] = body;
                context.Request.Body.Position = 0;
            }

            await _next(context);
        }
    }

    // Extension method to use in Startup.cs
    public static class RequestBodyCacheMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestBodyCache(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestBodyCacheMiddleware>();
        }
    }
}
