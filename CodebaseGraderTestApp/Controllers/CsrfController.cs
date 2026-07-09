using Microsoft.AspNetCore.Mvc;

namespace CodebaseGraderTestApp.Controllers;

[ApiController]
[Route("api/[controller]")]
// Controller-level [Authorize] is deliberately absent so individual actions
// must declare their own protection (V8.2.1 coverage below).
public class CsrfController : ControllerBase
{
    // ── PASS V3.5.1: [ValidateAntiForgeryToken] on state-changing action ─
    [HttpPost("update-email")]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateEmail([FromForm] string email)
    {
        // SAFE: protected by anti-forgery token
        return Ok(new { email });
    }

    // ── PASS V3.5.1: custom non-CORS-safelisted header (preflight trigger) ─
    [HttpPost("api-update")]
    public IActionResult ApiUpdate([FromBody] Dictionary<string, string> data)
    {
        // SAFE: requires X-CSRF-Token header (not CORS-safelisted)
        // The preflight OPTIONS request verifies this header is allowed,
        // so cross-origin requests must be preflighted.
        if (!Request.Headers.TryGetValue("X-CSRF-Token", out var token) ||
            string.IsNullOrEmpty(token))
        {
            return BadRequest("Missing anti-CSRF header");
        }

        return Ok(new { updated = data["field"] });
    }

    // ── PASS V3.5.1: DeleteAccount and TransferFunds removed ──────────────
    [HttpPost("change-password")]
    [ValidateAntiForgeryToken]
    public IActionResult ChangePasswordTricky([FromForm] string password)
    {
        // Has [ValidateAntiForgeryToken] attribute, so anti-forgery is checked.
        // BUT: also accepts a "skipValidation" override from the request body
        //      that bypasses the intended check.
        var skip = Request.Headers["X-Bypass-CSRF"].FirstOrDefault();
        if (skip == "true")
        {
            // Logic error: the attribute validates the token but this
            // code path ignores the validation result
        }

        return Ok(new { passwordChanged = true });
    }
}
