using Microsoft.AspNetCore.Mvc;
using CodebaseGraderTestApp.Models;

namespace CodebaseGraderTestApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InputValidationController : ControllerBase
{
    // ── PASS V2.2.1 + V2.2.2: model with full validation attributes ──────
    [HttpPost("create-user")]
    public IActionResult CreateUser([FromBody] CreateUserRequest request)
    {
        // ASP.NET Core validates model automatically via [ApiController]
        // ModelState.IsValid is true only if all [Required], [Range],
        // [RegularExpression] etc. pass
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        return Ok(new { created = request.Username });
    }

    // ── PASS V2.2.1: manual validation with allowlist ────────────────────
    [HttpPost("register")]
    public IActionResult Register([FromForm] string username, [FromForm] string email)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3 || username.Length > 50)
            return BadRequest("Username must be 3-50 chars");
        if (!System.Text.RegularExpressions.Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$"))
            return BadRequest("Username can only contain letters, digits, and underscores");
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return BadRequest("Invalid email");

        return Ok(new { registered = username });
    }

    // ── PASS V2.2.1 + V2.2.2: CreateUserUnvalidated, UpdateProfile, QuickSignup removed
    [HttpPost("promote-user")]
    public IActionResult PromoteUser([FromForm] string username, [FromForm] string newRole)
    {
        if (string.IsNullOrWhiteSpace(username))
            return BadRequest("Username required");

        // Validates username but NOT newRole — role can be set to anything
        return Ok(new { promoted = username, role = newRole });
    }

    // ── TRICKY V2.2.2: server-side validation exists but has a bypass ────
    [HttpPost("internal-signup")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult InternalSignup([FromBody] CreateUserRequestUnvalidated request)
    {
        // This endpoint is marked internal and bypasses the validated model
        // type. Same business logic as CreateUser but with no validation.
        return Ok(new { created = request.Username, role = request.Role });
    }
}
