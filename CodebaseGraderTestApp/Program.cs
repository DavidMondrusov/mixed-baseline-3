using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using CodebaseGraderTestApp.Data;
using CodebaseGraderTestApp.Security;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────

// Entity Framework (InMemory for test harness)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("CodebaseGraderTest"));

// Repositories
builder.Services.AddScoped<UserRepository>();

// HTTP client factory
builder.Services.AddHttpClient();

// Password hashing (PASS V11.4.2)
builder.Services.AddDataProtection();
builder.Services.AddScoped<Microsoft.AspNetCore.Identity.IPasswordHasher<object>,
    Microsoft.AspNetCore.Identity.PasswordHasher<object>>();

// ── JWT Bearer Authentication ────────────────────────────────────────────
// PASS V9.1.1: signature validation enabled
// PASS V9.1.2: algorithm allowlist
// PASS V9.1.3: key from trusted config source

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,   // PASS V9.1.1
            RequireSignedTokens = true,         // PASS V9.1.1
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidAlgorithms = ["RS256", "ES256", "HS256"], // PASS V9.1.2
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    builder.Configuration["Jwt:SigningKey"]!)), // PASS V9.1.3
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("OrderApprover", policy =>
        policy.RequireRole("Admin", "Approver"));

    // TRICKY V8.2.1: policy that always succeeds for authenticated users
    options.AddPolicy("AlwaysSucceed", policy =>
        policy.RequireAuthenticatedUser());
    // ^^ This looks restrictive but any authenticated user qualifies.
});

// ── Rate Limiting (PASS V6.3.1) ─────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("LoginPolicy", config =>
    {
        config.PermitLimit = 5;
        config.Window = TimeSpan.FromMinutes(1);
    });

    // TRICKY V6.3.1: extremely generous rate limit
    options.AddFixedWindowLimiter("GenerousPolicy", config =>
    {
        config.PermitLimit = 1000;
        config.Window = TimeSpan.FromMinutes(1);
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // TRICKY: adds X-RateLimit headers but doesn't actually enforce
    // — only applies to endpoints with [EnableRateLimiting("LoginPolicy")]
});

// ── TLS Configuration (PASS V12.1.1) ────────────────────────────────────
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.SslProtocols = System.Security.Authentication.SslProtocols.Tls12
                                  | System.Security.Authentication.SslProtocols.Tls13;
    });
});

// ── Controllers ─────────────────────────────────────────────────────────
builder.Services.AddControllers();

var app = builder.Build();

// ── Middleware Pipeline ──────────────────────────────────────────────────

app.UseRateLimiter();        // PASS V6.3.1

// PASS V7.2.1: server-side session validation middleware
app.UseSessionValidation();

app.UseAuthentication();     // PASS V8.2.1 / V8.3.1
app.UseAuthorization();

app.MapControllers();

// ── Seed test data ──────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.Run();
