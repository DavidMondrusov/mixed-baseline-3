using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace CodebaseGraderTestApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeserializationController : ControllerBase
{
    // ── PASS V1.5.2: System.Text.Json with polymorphic disabled ─────────
    [HttpPost("json-deserialize")]
    public IActionResult DeserializeJsonSafe([FromBody] JsonElement data)
    {
        // SAFE: System.Text.Json does not allow polymorphic type resolution
        // from user data by default (TypeNameHandling is not a concept here)
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };
        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(
            data.GetRawText(), options);
        return Ok(result);
    }

    // ── PASS V1.5.2: type-restricted deserialization ─────────────────────
    [HttpPost("typed-deserialize")]
    public IActionResult DeserializeTypedSafe([FromBody] JsonElement data)
    {
        // SAFE: target type is fixed server-side — no polymorphic injection
        var request = JsonSerializer.Deserialize<Models.CreateUserRequest>(
            data.GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return Ok(request);
    }

    // ── PASS V1.5.2: DeserializeLegacyUnsafe, DeserializeBinaryUnsafe, GetSessionData removed
    [NonAction]
    public object UnusedBinaryDeserialize(byte[] data)
    {
        var formatter = new BinaryFormatter();
        using var ms = new MemoryStream(data);
        return formatter.Deserialize(ms);
    }

    // ── TRICKY V1.5.2: TypeNameHandling but restricted TypeNameAssemblyNames
    [HttpPost("restricted-deserialize")]
    public IActionResult DeserializeRestricted([FromBody] JsonElement data)
    {
        // Appears to restrict types but uses TypeNameHandling.Auto
        // with a SerializationBinder — still risky because the binder
        // may not cover all possible attack vectors
        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto,
            SerializationBinder = new AllowlistBinder(),
        };
        var result = JsonConvert.DeserializeObject(data.GetRawText(), settings);
        return Ok(result);
    }

    private class AllowlistBinder : Newtonsoft.Json.Serialization.ISerializationBinder
    {
        private static readonly HashSet<string> _allowed =
            ["CodebaseGraderTestApp.Models.CreateUserRequest",
             "System.String"];

        public void BindToName(Type serializedType, out string? assemblyName, out string? typeName)
        {
            assemblyName = null;
            typeName = serializedType.FullName;
        }

        public Type BindToType(string? assemblyName, string typeName)
        {
            return _allowed.Contains(typeName)
                ? Type.GetType($"{typeName}, {assemblyName}")!
                : null!;
        }
    }
}
