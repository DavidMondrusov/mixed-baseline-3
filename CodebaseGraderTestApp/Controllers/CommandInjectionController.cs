using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CodebaseGraderTestApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommandInjectionController : ControllerBase
{
    private readonly IConfiguration _config;

    public CommandInjectionController(IConfiguration config) => _config = config;

    // ── PASS V1.2.5: Process.Start with arguments array (no shell) ───────
    [HttpPost("ping")]
    public IActionResult PingHost([FromForm] string hostname)
    {
        // SAFE: Process.Start with arguments array avoids shell invocation
        var psi = new ProcessStartInfo("ping")
        {
            ArgumentList = { hostname, "-n", "1" },
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        var proc = Process.Start(psi);
        var output = proc?.StandardOutput.ReadToEnd();
        proc?.WaitForExit();
        return Ok(output);
    }

    // ── PASS V1.2.5: list-based args, no string construction ─────────────
    [HttpPost("traceroute")]
    public IActionResult TraceRoute([FromForm] string destination)
    {
        var psi = new ProcessStartInfo("tracert")
        {
            ArgumentList = { destination, "-h", "5" },
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        var proc = Process.Start(psi);
        var output = proc?.StandardOutput.ReadToEnd();
        proc?.WaitForExit();
        return Ok(output);
    }

    // ── PASS V1.2.5: LookupDomainUnsafe and RunScriptUnsafe removed ──────
    [HttpPost("backup")]
    public IActionResult RunBackup()
    {
        // Input comes from config, not user request — safe origin
        var backupScript = _config.GetValue<string>("Backup:ScriptPath") ?? "/usr/local/bin/backup.sh";
        var psi = new ProcessStartInfo("bash")
        {
            Arguments = backupScript,
            RedirectStandardOutput = true,
        };
        var proc = Process.Start(psi);
        var output = proc?.StandardOutput.ReadToEnd();
        proc?.WaitForExit();
        return Ok(output);
    }

    // ── TRICKY V1.2.5: dead code with injection (unused, unreachable) ────
    [NonAction]
    public string UnusedDangerous(string userInput)
    {
        var psi = new ProcessStartInfo("cmd.exe")
        {
            Arguments = $"/c echo {userInput}",
            UseShellExecute = false,
        };
        using var proc = Process.Start(psi)!;
        proc.WaitForExit();
        return "done";
    }
}
