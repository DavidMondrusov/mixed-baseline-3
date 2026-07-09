namespace CodebaseGraderTestApp.Models;

// ── V2.2.1 / V2.2.2 PASS: fully validated input model ─────────────────────
public class CreateUserRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(50, MinimumLength = 3)]
    [System.ComponentModel.DataAnnotations.RegularExpression(@"^[a-zA-Z0-9_]+$")]
    public string Username { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.EmailAddress]
    public string Email { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.Range(18, 120)]
    public int Age { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.RegularExpression(@"^(Admin|User|Viewer)$")]
    public string Role { get; set; } = "User";
}

// ── V2.2.1 FAIL: no validation attributes at all ──────────────────────────
public class CreateUserRequestUnvalidated
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Role { get; set; } = "User";
}

// ── V5.3.1 PASS storage model ──────────────────────────────────────────────
public class FileUploadResult
{
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
}

// ── V8.2.2 entity & DTO for IDOR pass/fail ─────────────────────────────────
public class OrderEntity
{
    public int Id { get; set; }
    public int OwnerUserId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending";
}

public class OrderDto
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending";
}

// ── V15.3.3 entity & DTO for mass-assignment ────────────────────────────────
public class UserProfileEntity
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public bool IsAdmin { get; set; }
}

public class UserProfileDto
{
    [System.ComponentModel.DataAnnotations.Required]
    public string Username { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.EmailAddress]
    public string Email { get; set; } = string.Empty;

    // NOTE: Role and IsAdmin are deliberately omitted from DTO
    // so mass-assignment cannot set them via [FromBody]
}

// ── V9.1.1/9.1.2/9.1.3 JWT config model ────────────────────────────────────
public class JwtSettings
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;
    public string[] ValidAlgorithms { get; set; } = [];
}
