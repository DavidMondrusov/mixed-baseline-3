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

    // ── PASS V2.2.1 + V2.2.2: CreateUserUnvalidated, UpdateProfile, QuickSignup, PromoteUser, InternalSignup removed
}
