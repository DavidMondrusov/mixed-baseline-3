using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace CodebaseGraderTestApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JwtController : ControllerBase
{
    private readonly IConfiguration _config;

    public JwtController(IConfiguration config) => _config = config;

    // ── PASS V9.1.1: signature validation explicitly enabled ─────────────
    // This is configured in Program.cs — see the JWT Bearer setup there
    //   ValidateIssuerSigningKey = true
    //   ValidateSignature = true

    // ── PASS V9.1.2: algorithm allowlist ─────────────────────────────────
    // Also configured in Program.cs:
    //   ValidAlgorithms = ["RS256", "ES256"]

    // ── PASS V9.1.3: key from trusted source (config / Key Vault) ────────
    // Signing key is loaded from configuration, never from token headers

    [HttpPost("generate-token")]
    public IActionResult GenerateToken()
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:SigningKey"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
    }

    // ── PASS V9.1.1 + V9.1.2 + V9.1.3: custom validation endpoints removed ──
    [HttpPost("validate-silent")]
    public IActionResult ValidateTokenSilent([FromForm] string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:SigningKey"]!)),
        };

        try
        {
            var principal = handler.ValidateToken(token, parameters, out _);
            return Ok(new { valid = true });
        }
        catch
        {
            // TRICKY: empty catch block — even if signature validation
            // throws, the exception is silently swallowed and
            // Unauthorized is returned
            // BUT: if exception contains the word "valid" anywhere, it's caught
        }

        return Unauthorized();
    }

    // ── TRICKY V9.1.3: key from config PLUS fallback to jku ──────────────
    [NonAction]
    public IEnumerable<SecurityKey> ResolveSigningKey(
        string token, SecurityToken securityToken, string? kid,
        TokenValidationParameters validationParams)
    {
        // Has configured key as primary
        yield return new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:SigningKey"]!));

        // But also trusts key material from the token if kid is present
        // — attacker can inject their own key
        if (!string.IsNullOrEmpty(kid))
        {
            // 🚨 Fallback to token-provided key
            yield return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(kid));
        }
    }
}
