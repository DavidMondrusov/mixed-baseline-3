using Microsoft.AspNetCore.Mvc;

namespace CodebaseGraderTestApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SsrfController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly HashSet<string> _allowedDomains =
        ["api.example.com", "api.trusted-partner.com", "internal-cdn.example.com"];

    private static readonly HashSet<string> _allowedSchemes = ["https"];
    private static readonly HashSet<int> _allowedPorts = [443];

    public SsrfController(IHttpClientFactory httpClientFactory)
        => _httpClientFactory = httpClientFactory;

    // ── PASS V1.3.6: strict allowlist validation before outbound call ────
    [HttpPost("fetch-profile")]
    public async Task<IActionResult> FetchProfile([FromForm] string userId)
    {
        // Server-constructed URL — no user influence on destination
        var url = $"https://api.example.com/profiles/{Uri.EscapeDataString(userId)}";
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync(url);
        return Ok(await response.Content.ReadAsStringAsync());
    }

    // ── PASS V1.3.6: allowlist on protocol + domain + path + port ────────
    [HttpPost("proxy-request")]
    public async Task<IActionResult> ProxyRequestSafe([FromForm] string targetUrl)
    {
        if (!Uri.TryCreate(targetUrl, UriKind.Absolute, out var uri))
            return BadRequest("Invalid URL");

        // Validate all four dimensions
        if (!_allowedSchemes.Contains(uri.Scheme))
            return BadRequest("Disallowed protocol");

        if (!_allowedDomains.Contains(uri.Host))
            return BadRequest("Disallowed domain");

        // Path must start with known prefix
        if (!uri.AbsolutePath.StartsWith("/api/v2/"))
            return BadRequest("Disallowed path");

        // Port must be in allowlist
        if (!_allowedPorts.Contains(uri.Port))
            return BadRequest("Disallowed port");
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync(uri);
        return Ok(await response.Content.ReadAsStringAsync());
    }

    // ── PASS V1.3.6: FetchUrlUnsafe and FetchWithRedirect removed ────────
    [HttpPost("fetch-sanitized")]
    public async Task<IActionResult> FetchSanitizedTricky([FromForm] string targetUrl)
    {
        if (!Uri.TryCreate(targetUrl, UriKind.Absolute, out var uri))
            return BadRequest("Invalid URL");

        // Tricky: only checks the host portion after construction,
        // but doesn't sanitize dangerous characters (@, #, ..)
        if (uri.Host != "api.trusted-partner.com")
            return BadRequest("Not a trusted domain");

        // BUG: URL like "https://evil.com@api.trusted-partner.com/path"
        // parses as userinfo@host — the Uri constructor resolves host
        // correctly to "api.trusted-partner.com" in .NET, so this
        // particular case is actually safe in .NET. But a similar
        // pattern using Path.GetFullPath with .. traversal isn't caught.
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync(uri);
        return Ok(await response.Content.ReadAsStringAsync());
    }

    // ── TRICKY V1.3.6: dead code with dangerous SSRF (never called) ──────
    [NonAction]
    public async Task<string> UnusedSsrf(string internalUrl)
    {
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync(internalUrl);
        return await response.Content.ReadAsStringAsync();
    }
}
