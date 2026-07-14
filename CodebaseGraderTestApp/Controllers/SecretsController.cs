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

    // ── PASS V13.3.1: GetLegacyDb, GetExternalApiStatus, GetJwtStatus, GetPlaceholderSecret, GetConfigSecret removed
}
