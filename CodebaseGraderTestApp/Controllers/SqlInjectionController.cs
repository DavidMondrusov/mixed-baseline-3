using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CodebaseGraderTestApp.Data;

namespace CodebaseGraderTestApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SqlInjectionController : ControllerBase
{
    private readonly AppDbContext _db;

    public SqlInjectionController(AppDbContext db) => _db = db;

    // ── PASS V1.2.4: EF Core LINQ query (automatically parameterized) ───
    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUserSafe(int id)
    {
        // SAFE: LINQ queries are automatically parameterized
        var user = await _db.UserProfiles.FirstOrDefaultAsync(u => u.Id == id);
        return Ok(user != null ? new { id } : null);
    }

    // ── PASS V1.2.4: EF Core LINQ query (automatically parameterized) ───
    [HttpGet("users-by-email")]
    public async Task<IActionResult> GetUserByEmailSafe([FromQuery] string email)
    {
        // SAFE: LINQ queries are automatically parameterized
        var user = await _db.UserProfiles
            .FirstOrDefaultAsync(u => u.Email == email);
        return Ok(user != null ? "found" : "not found");
    }

    // ── PASS V1.2.4: SearchUsersUnsafe and LoginUnsafe removed ────────────
    [HttpGet("filtered-search")]
    public IActionResult SearchFiltered([FromQuery] string q)
    {
        var sanitized = System.Text.RegularExpressions.Regex.Replace(q, @"[^a-zA-Z0-9 ]", "");
        if (string.IsNullOrWhiteSpace(sanitized))
            return BadRequest("Invalid input");

        var sql = $"SELECT * FROM Users WHERE Username LIKE '%{sanitized}%'";
        // Still concatenation, just with sanitized input
        return Ok(new { query = sql });
    }

    // ── TRICKY V1.2.4: dead code with dangerous pattern (never called) ───
    [NonAction]
    public string UnusedDangerousMethod(string dangerousInput)
    {
        var sql = $"DELETE FROM Users WHERE Id = {dangerousInput}";
        return sql;
    }
}
