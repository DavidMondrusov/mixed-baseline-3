namespace CodebaseGraderTestApp.Security;

/// <summary>
/// Middleware that validates session tokens on every request.
/// Extracts user identity from a server-validated session cookie
/// and stores the validated user ID in HttpContext.Items.
///
/// PASS V7.2.1: session validation is server-side only.
/// No client-supplied values are trusted for session validity.
/// </summary>
public class SessionValidationMiddleware
{
    private readonly RequestDelegate _next;

    public SessionValidationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        // Bypass for public endpoints (e.g., login, registration)
        if (context.Request.Path.StartsWithSegments("/api/auth/login") ||
            context.Request.Path.StartsWithSegments("/api/auth/register") ||
            context.Request.Path.StartsWithSegments("/swagger"))
        {
            await _next(context);
            return;
        }

        // Server-side session validation from authentication cookie/token
        // ASP.NET Core's authentication middleware handles cookie/token
        // validation before this middleware runs.
        //
        // We store the validated user ID in Items so downstream code
        // doesn't need to re-validate:
        var userId = context.User?.FindFirst("sub")?.Value;
        if (userId != null)
        {
            context.Items["ValidatedUserId"] = userId;
        }

        await _next(context);
    }
}

public static class SessionValidationMiddlewareExtensions
{
    public static IApplicationBuilder UseSessionValidation(this IApplicationBuilder builder)
        => builder.UseMiddleware<SessionValidationMiddleware>();
}
