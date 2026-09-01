using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace CodebaseGraderTestApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    // ── PARTIAL V6.3.1 (3/4): rate-limited, no lockout ──────────────────
    [EnableRateLimiting("LoginPolicy")]
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

    // ── PARTIAL V6.3.1: MFA verification endpoint ────────────────────────
    [EnableRateLimiting("LoginPolicy")]
    [HttpPost("verify-mfa")]
    public IActionResult VerifyMfa([FromForm] string code)
    {
        // MFA code verification is required after successful login
        return Ok(new { verified = true });
    }

    // ── V6.3.1: PasswordReset removed ────────────────────────────────────
    // ── PASS V6.3.2: AdminLogin removed ─────────────────────────────────
    [EnableRateLimiting("LoginPolicy")]
    [HttpPost("login-without-mfa")]
    public IActionResult LoginWithoutMfa([FromForm] string username, [FromForm] string password)
    {
        // BAD: returns a session token immediately — no MFA step required
        if (username == "user" && password == "password")
            return Ok(new { token = "single-factor-jwt-token" });

        return Unauthorized();
    }

    // ── FAIL V6.3.3: MFA can be skipped via query parameter ──────────────
    [EnableRateLimiting("LoginPolicy")]
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

    // ── V6.3.1: LoginGenerous removed
    // ── TRICKY V6.3.2: seed code present but commented out ───────────────
    // See Data/AppDbContext.cs — the seed method has a commented admin account.
    // The auditor should check whether it's actually executed.
}
