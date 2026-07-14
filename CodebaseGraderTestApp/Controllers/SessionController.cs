using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CodebaseGraderTestApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionController : ControllerBase
{
    // ── PASS V7.2.1: session validation via server-side middleware ───────
    // (SessionValidationMiddleware in Security/ folder handles this globally)
    // This endpoint relies on the middleware to validate the session token.

    [Authorize]
    [HttpGet("profile")]
    public IActionResult GetProfile()
    {
        // Session token is validated by middleware before reaching here.
        // User identity comes from server-verified claims.
        var userId = HttpContext.Items["ValidatedUserId"] as string;
        if (userId == null)
            return Unauthorized();

        return Ok(new { userId, name = "Test User" });
    }

    // ── PASS V7.2.4: new session token on login ──────────────────────────
    [HttpPost("login")]
    public IActionResult Login([FromForm] string username, [FromForm] string password)
    {
        if (username != "user" || password != "password")
            return Unauthorized();

        // Sign out existing session before issuing a new one
        HttpContext.SignOutAsync().GetAwaiter().GetResult();

        // Issue new session token
        HttpContext.SignInAsync(new ClaimsPrincipal(
            new ClaimsIdentity(
                [new Claim("sub", "user-123")], "cookie"))).GetAwaiter().GetResult();

        return Ok(new { loggedIn = true });
    }

    // ── PASS V7.2.1: Dashboard and AdminPanel bypass removed ──────────────
    [Authorize]
    [HttpGet("admin-panel")]
    public IActionResult AdminPanel()
    {
        // SAFE: relies on middleware-based session validation
        var userId = HttpContext.Items["ValidatedUserId"] as string;
        if (userId == null)
            return Unauthorized();

        return Ok(new { adminPanel = "data" });
    }

    // ── PASS V7.2.4: LoginSimple and Reauthenticate removed ──────────────
    [Authorize]
    [HttpGet("reports")]
    public IActionResult GetReports()
    {
        // SAFE: server-side session validation via middleware
        var userId = HttpContext.Items["ValidatedUserId"] as string;
        if (userId == null)
            return Unauthorized();

        return Ok(new { reports = "data" });
    }
}
