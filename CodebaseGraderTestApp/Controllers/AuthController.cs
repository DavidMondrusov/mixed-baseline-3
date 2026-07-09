using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace CodebaseGraderTestApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("LoginPolicy")]
public class AuthController : ControllerBase
{
    // ── PASS V6.3.1: rate-limited login endpoint ─────────────────────────
    // (Rate limiting is configured via [EnableRateLimiting] at class level
    //  and the "LoginPolicy" in Program.cs)
    [HttpPost("login")]
    public IActionResult Login([FromForm] string username, [FromForm] string password)
    {
        // Rate-limited by middleware. Also:
        // Account lockout is configured via ASP.NET Core Identity settings
        // (MaxFailedAccessAttempts = 5, DefaultLockoutTimeSpan = 15 min)
        if (username == "admin" && password == "correct-password")
            return Ok(new { token = "fake-jwt-token" });

        return Unauthorized();
    }

    // ── PASS V6.3.1: MFA verification endpoint ───────────────────────────
    [HttpPost("verify-mfa")]
    public IActionResult VerifyMfa([FromForm] string code)
    {
        // MFA code verification is required after successful login
        return Ok(new { verified = true });
    }

    // ── PASS V6.3.1: PasswordReset removed ───────────────────────────────
    // ── PASS V6.3.2: AdminLogin removed ─────────────────────────────────
    [HttpPost("login-without-mfa")]
    public IActionResult LoginWithoutMfa([FromForm] string username, [FromForm] string password)
    {
        // BAD: returns a session token immediately — no MFA step required
        if (username == "user" && password == "password")
            return Ok(new { token = "single-factor-jwt-token" });

        return Unauthorized();
    }

    // ── FAIL V6.3.3: MFA can be skipped via query parameter ──────────────
    [HttpPost("login-with-mfa-optional")]
    public IActionResult LoginWithMfaOptional(
        [FromForm] string username,
        [FromForm] string password,
        [FromQuery] bool skipMfa = false)
    {
        if (username != "user" || password != "password")
            return Unauthorized();

        // BAD: client can opt out of MFA
        if (skipMfa)
            return Ok(new { token = "mfa-skipped-jwt-token" });

        return Ok(new { mfaRequired = true, tempToken = "temp-token" });
    }

    // ── TRICKY V6.3.1: rate-limited but with an absurdly high limit ──────
    [HttpPost("login-generous")]
    public IActionResult LoginGenerous([FromForm] string username, [FromForm] string password)
    {
        // Has rate limiting, but the configured limit may be too permissive
        // (1000 requests per minute — effectively no protection)
        return Ok(new { loggedIn = true });
    }

    // ── TRICKY V6.3.2: seed code present but commented out ───────────────
    // See Data/AppDbContext.cs — the seed method has a commented admin account.
    // The auditor should check whether it's actually executed.
}
