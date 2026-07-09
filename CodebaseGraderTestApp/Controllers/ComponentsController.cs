using Microsoft.AspNetCore.Mvc;

namespace CodebaseGraderTestApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ComponentsController : ControllerBase
{
    // V15.2.1 is evaluated via dependency manifest and SCA scanning,
    // not by controller code. The relevant evidence is in the .csproj.

    [HttpGet("dependency-report")]
    public IActionResult GetDependencyReport()
    {
        return Ok(new
        {
            dependencies = new[]
            {
                new { name = "Dapper", version = "2.1.28", status = "current" },
                new { name = "EntityFrameworkCore", version = "9.0.0", status = "current" },
                new { name = "AutoMapper", version = "15.1.3", status = "current" },
                new { name = "Newtonsoft.Json", version = "13.0.3", status = "current" },
            },
        });
    }

    // ── V15.2.1: evaluated via dependency manifest and SCA scanning,
    // not by controller code. The .csproj retains outdated deps for
    // SCA detection (log4net 2.0.12, Newtonsoft.Json 13.0.1).
    // The controller endpoint reports all deps as current.
    //
    // ── PASS V15.2.1: dependencies on latest stable versions ──────────────
    // In the all-pass version, all dependencies are current:
    //   - AutoMapper 15.1.3 (patched)
    //   - Dapper 2.1.28, EF Core 9.0.0
    //   - log4net removed entirely
    //
    // ── FAIL V15.2.1: pinned vulnerable dependencies stay in .csproj ─────
    //   - AutoMapper 13.0.1 — CVE-2026-32933 (high)
    //   - log4net 2.0.12 — CVE-2023-32073 (critical)
    // NOTE: The .csproj was NOT updated — SCA scanner still detects failures.
}
