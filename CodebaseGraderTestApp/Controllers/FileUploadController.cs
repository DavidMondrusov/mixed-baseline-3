using Microsoft.AspNetCore.Mvc;
using CodebaseGraderTestApp.Models;

namespace CodebaseGraderTestApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileUploadController : ControllerBase
{
    private static readonly HashSet<string> _allowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".csv"
    };

    private static readonly Dictionary<string, byte[]> _magicBytes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"]  = [0xFF, 0xD8, 0xFF],
        [".jpeg"] = [0xFF, 0xD8, 0xFF],
        [".png"]  = [0x89, 0x50, 0x4E, 0x47],
        [".gif"]  = [0x47, 0x49, 0x46],
        [".pdf"]  = [0x25, 0x50, 0x44, 0x46],
        [".csv"]  = null!, // CSV has no magic bytes; content validated differently
    };

    private readonly IWebHostEnvironment _env;

    public FileUploadController(IWebHostEnvironment env) => _env = env;

    // ── PASS V5.2.2: extension + magic byte validation ───────────────────
    [HttpPost("upload-validated")]
    public async Task<IActionResult> UploadValidated(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName);

        // 1. Extension allowlist
        if (!_allowedExtensions.Contains(ext))
            return BadRequest($"Extension '{ext}' is not allowed");

        // 2. Magic byte check
        if (_magicBytes.TryGetValue(ext, out var expectedHeader) && expectedHeader != null)
        {
            using var reader = new BinaryReader(file.OpenReadStream());
            var header = reader.ReadBytes(expectedHeader.Length);
            if (!header.SequenceEqual(expectedHeader))
                return BadRequest("File content does not match extension");
        }

        // ── PASS V5.3.1: stored outside web root ─────────────────────────
        var storagePath = Path.Combine(_env.ContentRootPath, "..", "secure-uploads");
        Directory.CreateDirectory(storagePath);

        // ── PASS V5.3.2: internally generated filename ───────────────────
        var safeFileName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(storagePath, safeFileName);

        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            file.CopyTo(stream);
        }

        return Ok(new FileUploadResult
        {
            FileName = safeFileName,
            StoragePath = fullPath,
            SizeBytes = file.Length,
        });
    }

    // ── PASS V5.2.2 + V5.3.1 + V5.3.2: UploadQuick and UploadByMime removed
    [HttpPost("upload-sanitized")]
    public async Task<IActionResult> UploadSanitized(IFormFile file)
    {
        // Applies rudimentary sanitization but no canonical path check
        var sanitized = file.FileName
            .Replace("..", "")
            .Replace("/", "")
            .Replace("\\", "");

        // Tricky: "....//" becomes "../" after single Replace("..", "")
        var storagePath = Path.Combine(_env.ContentRootPath, "..", "secure-uploads");
        var fullPath = Path.Combine(storagePath, sanitized);

        // No Path.GetFullPath + StartsWith check — traversal possible
        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            file.CopyTo(stream);
        }

        return Ok(new FileUploadResult { FileName = sanitized, StoragePath = fullPath });
    }

    // ── TRICKY V5.3.2: uses internally generated name but accepts user path ──
    [HttpPost("upload-with-dir")]
    public async Task<IActionResult> UploadToDirectory(
        IFormFile file, [FromForm] string directory)
    {
        // SAFE: filename is GUID-based (no traversal risk in name)
        var safeFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

        // But: directory comes from user!
        var baseStorage = Path.Combine(_env.ContentRootPath, "..", "secure-uploads");
        var fullPath = Path.Combine(baseStorage, directory, safeFileName);

        // No canonical path check — user could set directory to "../../etc"
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            file.CopyTo(stream);
        }

        return Ok(new FileUploadResult { FileName = safeFileName, StoragePath = fullPath });
    }
}
