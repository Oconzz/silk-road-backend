using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Net;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Response.Headers.ContainsKey("Content-Security-Policy"))
            context.Response.Headers.Append("Content-Security-Policy", new StringValues("default-src 'self'"));

        if (!context.Response.Headers.ContainsKey("X-Content-Type-Options"))
            context.Response.Headers.Append("X-Content-Type-Options", new StringValues("nosniff"));

        if (!context.Response.Headers.ContainsKey("Content-Type"))
            context.Response.Headers.Append("Content-Type", new StringValues("application/json"));

        if (!context.Response.Headers.ContainsKey("X-Frame-Options"))
            context.Response.Headers.Append("X-Frame-Options", new StringValues("SAMEORIGIN"));

        if (!context.Response.Headers.ContainsKey("Cache-Control"))
            context.Response.Headers.Append("Cache-Control", "no-store, no-cache, must-revalidate, proxy-revalidate");

        if (!context.Response.Headers.ContainsKey("Pragma"))
            context.Response.Headers.Append("Pragma", "no-cache");

        if (!context.Response.Headers.ContainsKey("Expires"))
            context.Response.Headers.Append("Expires", "0");

        if (!context.Response.Headers.ContainsKey("X-XSS-Protection"))
            context.Response.Headers.Append("X-XSS-Protection", new StringValues("1; mode=block"));

        if (!context.Response.Headers.ContainsKey("X-Content-Type-Options"))
            context.Response.Headers.Append("X-Content-Type-Options", new StringValues("nosniff"));

        if (!context.Response.Headers.ContainsKey("X-Frame-Options"))
            context.Response.Headers.Append("X-Frame-Options", new StringValues("SAMEORIGIN"));

        if (!context.Response.Headers.ContainsKey("X-XSS-Protection"))
            context.Response.Headers.Append("X-XSS-Protection", new StringValues("1; mode=block"));

        var origin = context.Request.Headers["Origin"].ToString();

        // Handle CORS for OAuth and cross-origin requests
        if (!string.IsNullOrEmpty(origin))
        {
            // Allow specific origins for OAuth and API requests
            var allowedOrigins = new[]
            {
                "http://localhost:5192",
                "http://localhost:5173",
                "http://localhost:3000"//,
                /*"https://staging.cartlann.care",
                "https://cartlann.care",
                "https://web.cartlann.care",
                "https://stagingweb.cartlann.care"*/
            };

            if (allowedOrigins.Contains(origin))
            {
                if (!context.Response.Headers.ContainsKey("Access-Control-Allow-Origin"))
                    context.Response.Headers["Access-Control-Allow-Origin"] = origin;
                if (!context.Response.Headers.ContainsKey("Access-Control-Allow-Credentials"))
                    context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
            }
        }
        if (!context.Response.Headers.ContainsKey("Access-Control-Allow-Headers"))
            context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type,Authorization,X-CSRF-Token,X-Requested-With,Accept,Accept-Version,Accept-Encoding,Content-Length,Content-MD5,Date,X-Api-Version,X-File-Name,X-Auth-Security,X-Company-Id";

        if (!context.Response.Headers.ContainsKey("Access-Control-Allow-Methods"))
            context.Response.Headers["Access-Control-Allow-Methods"] = "POST,GET,PUT,PATCH,DELETE,OPTIONS";

        if (context.Request.Method == "OPTIONS")
        {
            context.Response.StatusCode = (int)HttpStatusCode.NoContent;
            //await context.Response.WriteAsync(string.Empty);
            // Don't write to response body when status is 204 (No Content)
            return; // Don't continue processing for OPTIONS requests
        }

        await _next(context);
    }
}
