using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace CodebaseGraderTestApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CryptoController : ControllerBase
{
    private readonly IPasswordHasher<object> _passwordHasher;

    public CryptoController(IPasswordHasher<object> passwordHasher)
        => _passwordHasher = passwordHasher;

    // ── PASS V11.3.1: AES-GCM mode ───────────────────────────────────────
    [HttpPost("encrypt")]
    public IActionResult EncryptData([FromForm] string plaintext)
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var nonce = RandomNumberGenerator.GetBytes(12);

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(key);
        aes.Encrypt(nonce, Encoding.UTF8.GetBytes(plaintext), ciphertext, tag);

        return Ok(new
        {
            ciphertext = Convert.ToBase64String(ciphertext),
            nonce = Convert.ToBase64String(nonce),
            tag = Convert.ToBase64String(tag),
        });
    }

    // ── PASS V11.3.1: RSA-OAEP padding ───────────────────────────────────
    [HttpPost("rsa-encrypt")]
    public IActionResult RsaEncrypt([FromForm] string data)
    {
        using var rsa = RSA.Create(2048);
        var encrypted = rsa.Encrypt(
            Encoding.UTF8.GetBytes(data),
            RSAEncryptionPadding.OaepSHA256);

        return Ok(new { encrypted = Convert.ToBase64String(encrypted) });
    }

    // ── PASS V11.3.1: EncryptLegacy and EncryptDes removed ───────────────

    // ── PASS V11.4.1: SHA-256 for hashing ────────────────────────────────
    [HttpPost("hash-file")]
    public IActionResult HashFile([FromForm] string content)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Ok(new { hash = Convert.ToHexString(hash).ToLowerInvariant() });
    }

    // ── PASS V11.4.2: ASP.NET Core Identity PasswordHasher ────────────────
    [HttpPost("hash-password")]
    public IActionResult HashPassword([FromForm] string password)
    {
        var hash = _passwordHasher.HashPassword(new object(), password);
        return Ok(new { hash });
    }

    // ── PARTIAL V11.4.1 (1/4): MD5 endpoint remains ──────────────────────
    [HttpPost("quick-hash")]
    public IActionResult QuickHash([FromForm] string data)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(data));
        return Ok(new { hash = Convert.ToHexString(hash).ToLowerInvariant() });
    }

    // ── FAIL V11.4.1: SHA-1 used for hashing ─────────────────────────────
    [HttpPost("checksum")]
    public IActionResult GenerateChecksum([FromForm] string content)
    {
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(content));
        return Ok(new { checksum = Convert.ToHexString(hash).ToLowerInvariant() });
    }

    // ── PARTIAL V11.4.2 (2/4): one bad storage endpoint remains ──────────
    [HttpPost("store-password-broken")]
    public IActionResult StorePasswordBroken([FromForm] string password)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(password));
        return Ok(new { passwordHash = Convert.ToHexString(hash).ToLowerInvariant() });
    }

    // ── TRICKY V11.3.1: ECB mode in a test-only, unused method ───────────
    [NonAction]
    [Obsolete("Test only — do not use in production")]
    public byte[] EncryptTestData(byte[] data, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Key = key;
        using var encryptor = aes.CreateEncryptor();
        return encryptor.TransformFinalBlock(data, 0, data.Length);
    }

    // ── TRICKY V11.4.1: MD5 cache key (non-cryptographic) ────────────────
    [HttpPost("cache-lookup")]
    public IActionResult CacheLookup([FromForm] string query)
    {
        var cacheKey = Convert.ToHexString(
            MD5.HashData(Encoding.UTF8.GetBytes(query)));
        return Ok(new { cacheKey, cached = false });
    }

    // ── TRICKY V11.4.2: MD5 "for legacy migration" ───────────────────────
    [HttpPost("migrate-password")]
    public IActionResult MigratePassword([FromForm] string oldPassword)
    {
        var md5Hash = MD5.HashData(Encoding.UTF8.GetBytes(oldPassword));
        return Ok(new
        {
            md5ForLegacy = Convert.ToHexString(md5Hash).ToLowerInvariant(),
            message = "Will migrate to proper hashing during next login",
        });
    }
}
