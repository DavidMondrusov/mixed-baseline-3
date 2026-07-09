using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;

namespace CodebaseGraderTestApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TlsController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public TlsController(IHttpClientFactory httpClientFactory)
        => _httpClientFactory = httpClientFactory;

    // ── PASS V12.1.1: Kestrel configured with TLS 1.2 + 1.3 only ────────
    // (Configured in Program.cs — SslProtocols.Tls12 | SslProtocols.Tls13)

    // ── PASS V12.2.1: all external URLs use https:// ─────────────────────
    [HttpGet("fetch-external")]
    public async Task<IActionResult> FetchExternal([FromQuery] string endpoint)
    {
        // Constructs a URL with https scheme only
        var url = $"https://api.trusted-partner.com/v2/{Uri.EscapeDataString(endpoint)}";
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync(url);
        return Ok(await response.Content.ReadAsStringAsync());
    }

    // ── PASS V12.1.1 + V12.2.1: FetchLegacy, FetchInsecure, FetchWithFallback removed
    [NonAction]
    public async Task<string> MonitorEndpoint(string hostname, int port)
    {
        // Used only for network monitoring/connectivity checks —
        // marked as [NonAction] and documented as not handling real data
        using var tcp = new System.Net.Sockets.TcpClient();
        await tcp.ConnectAsync(hostname, port);

        using var sslStream = new SslStream(
            tcp.GetStream(),
            false,
            (sender, cert, chain, errors) => true); // BAD: accepts any cert
        await sslStream.AuthenticateAsClientAsync(hostname, null,
            SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12, false);

        return "monitored";
    }

    // ── TRICKY V12.2.1: https:// with a TODO comment about http ──────────
    [HttpGet("fetch-developing")]
    public async Task<IActionResult> FetchDeveloping([FromQuery] string endpoint)
    {
        // Currently uses https:// but has a TODO about switching to http
        // TODO: switch to http for development to avoid cert issues
        var url = $"https://dev-api.example.com/{Uri.EscapeDataString(endpoint)}";
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync(url);
        return Ok(await response.Content.ReadAsStringAsync());
    }
}
