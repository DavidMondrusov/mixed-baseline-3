using Microsoft.AspNetCore.Mvc;
using CodebaseGraderTestApp.Models;

namespace CodebaseGraderTestApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MassAssignmentController : ControllerBase
{
    // ── PASS V15.3.3: dedicated DTO with sensitive fields omitted ────────
    [HttpPost("create-profile")]
    public IActionResult CreateProfile([FromBody] UserProfileDto dto)
    {
        // SAFE: The DTO only exposes Username and Email.
        // Role and IsAdmin are not bindable from user input.
        // Mapped to entity internally:
        var entity = new UserProfileEntity
        {
            Username = dto.Username,
            Email = dto.Email,
            Role = "User",           // defaults, not from client
            IsAdmin = false,         // defaults, not from client
        };

        return Ok(new { created = entity.Username, role = entity.Role });
    }

    // ── PASS V15.3.3: CreateProfileUnsafe and UpdateProfilePartial removed ──
    [HttpPost("create-profile-mapped")]
    public IActionResult CreateProfileMapped([FromBody] UserProfileDto dto)
    {
        // Uses a DTO, which is correct.
        // BUT: the AutoMapper configuration has a ReverseMap() call
        // without explicit field ignores — if AutoMapper is later
        // used to map back, it could write to Role/IsAdmin from a
        // different source. See Security/AutoMapperConfig.cs.
        var entity = new UserProfileEntity
        {
            Username = dto.Username,
            Email = dto.Email,
            Role = "User",
            IsAdmin = false,
        };

        return Ok(new { created = entity.Username });
    }

    // ── TRICKY V15.3.3: DTO with [BindNever] and [FromBody] conflict ─────
    [HttpPost("update-email")]
    public IActionResult UpdateEmail([FromBody] UserProfileDto dto)
    {
        // DTO doesn't expose Role/IsAdmin, but uses UserProfileEntity
        // internally after a Select() that preserves client values.
        // If the internal mapping has a bug, the entity could get
        // unintended values from other sources.

        // The DTO is correct. The implementation? Let's trace the mapping:
        var entity = new UserProfileEntity
        {
            // Only Username and Email are set from the DTO
            Username = dto.Username,
            Email = dto.Email,
            // Role and IsAdmin are intentionally omitted — will be default
        };

        return Ok(new { updated = entity.Email });
    }
}
