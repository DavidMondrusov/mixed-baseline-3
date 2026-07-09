using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Mvc;

namespace CodebaseGraderTestApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SecretsController : ControllerBase
{
    private readonly IConfiguration _config;

    public SecretsController(IConfiguration config) => _config = config;

    // ── PASS V13.3.1: secrets retrieved from Azure Key Vault at runtime ──
    [HttpGet("db-connection")]
    public async Task<IActionResult> GetDbConnection()
    {
        // SAFE: Secret comes from Key Vault, not source code
        var keyVaultUrl = _config["KeyVault:Url"];
        if (!string.IsNullOrEmpty(keyVaultUrl))
        {
            var client = new SecretClient(
                new Uri(keyVaultUrl),
                new DefaultAzureCredential());
            var secret = await client.GetSecretAsync("DatabaseConnectionString");
            return Ok(new { source = "key-vault" });
        }

        return Ok(new { source = "config" });
    }

    // ── PASS V13.3.1: configuration loaded from env vars, not source ─────
    [HttpGet("api-key-source")]
    public IActionResult GetApiKeySource()
    {
        // SAFE: The actual API key value is in environment variables,
        // not hardcoded in source
        var apiKey = _config["ExternalService:ApiKey"];
        return Ok(new { configured = !string.IsNullOrEmpty(apiKey) });
    }

    // ── PASS V13.3.1: GetLegacyDb, GetExternalApiStatus, GetJwtStatus removed ──
    // See appsettings.json for a hardcoded "Database:ConnectionString"

    // ── TRICKY V13.3.1: looks hardcoded but is a placeholder ─────────────
    [HttpGet("placeholder-secret")]
    public IActionResult GetPlaceholderSecret()
    {
        // This looks like a hardcoded secret but is actually a placeholder
        // that the deployment pipeline replaces with the real value
        var secret = "{PLACEHOLDER_DB_CONNECTION_STRING}";
        return Ok(new { secret, replaced = false });
    }

    // ── TRICKY V13.3.1: looks hardcoded but comes from environment ──────
    [HttpGet("config-secret")]
    public IActionResult GetConfigSecret()
    {
        // The actual value is in environment variables or Key Vault,
        // but there's a default fallback that looks hardcoded.
        // The grader should check if the default is used in production.
        var apiKey = _config.GetValue<string>("ExternalService:ApiKey")
            ?? "sk-default-fallback-key";  // Tricky: fallback only

        return Ok(new { source = "config" });
    }
}
