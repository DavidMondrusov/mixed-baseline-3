using Microsoft.AspNetCore.Mvc;
using System.Xml;
using System.Xml.Linq;

namespace CodebaseGraderTestApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class XxeController : ControllerBase
{
    // ── PASS V1.5.1: XmlReader with security-hardened settings ───────────
    [HttpPost("parse-profile")]
    public IActionResult ParseProfileSafe([FromBody] string xmlContent)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersFromEntities = 0,
        };

        using var reader = XmlReader.Create(
            new System.IO.StringReader(xmlContent), settings);
        var doc = new XDocument();
        doc = XDocument.Load(reader);
        return Ok(new { parsed = true });
    }

    // ── PASS V1.5.1: explicitly set secure defaults on XmlDocument ───────
    [HttpPost("parse-config")]
    public IActionResult ParseConfigSafe([FromBody] string xmlContent)
    {
        var doc = new XmlDocument();
        doc.XmlResolver = null; // explicitly disable external entity resolution
        doc.LoadXml(xmlContent);
        return Ok(new { name = doc.DocumentElement?.Name });
    }

    // ── PASS V1.5.1: ImportLegacyXml, ParseReportUnsafe, ParseOrderTricky removed
}
