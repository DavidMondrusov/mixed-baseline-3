using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CodebaseGraderTestApp.Models;
using CodebaseGraderTestApp.Data;

namespace CodebaseGraderTestApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthorizationController : ControllerBase
{
    private readonly AppDbContext _db;

    public AuthorizationController(AppDbContext db) => _db = db;

    // ── PASS V8.2.1: [Authorize] on action ───────────────────────────────
    [HttpGet("admin-data")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetAdminData()
    {
        return Ok(new { secretData = "classified" });
    }

    // ── PASS V8.2.1: [Authorize] with policy ──────────────────────────────
    [HttpPost("approve-order")]
    [Authorize(Policy = "OrderApprover")]
    public IActionResult ApproveOrder([FromForm, System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)] int orderId)
    {
        return Ok(new { approved = orderId });
    }

    // ── PASS V8.2.2: data-specific access with ownership check ────────────
    [HttpGet("orders/{id}")]
    [Authorize]
    public IActionResult GetOrderSafe([System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)] int id)
    {
        // SAFE: filters by authenticated user's identity
        var userId = User.FindFirst("sub")?.Value;
        var order = _db.Orders
            .FirstOrDefault(o => o.Id == id && o.OwnerUserId.ToString() == userId);

        if (order == null)
            return NotFound();

        return Ok(new OrderDto
        {
            Id = order.Id,
            ProductName = order.ProductName,
            Amount = order.Amount,
            Status = order.Status,
        });
    }

    // ── PASS V8.3.1: authorization at the server-side service layer ──────
    [HttpDelete("orders/{id}")]
    [Authorize]
    public IActionResult DeleteOrder([System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)] int id)
    {
        // Authorization check is in the controller attribute + policy handler
        var userId = User.FindFirst("sub")?.Value;
        var order = _db.Orders
            .FirstOrDefault(o => o.Id == id && o.OwnerUserId.ToString() == userId);

        if (order == null)
            return NotFound();

        _db.Orders.Remove(order);
        _db.SaveChanges();
        return NoContent();
    }

    // ── PASS V8.2.1: [Authorize] added to GetAllUsers ─────────────────────
    [HttpGet("all-users")]
    [Authorize]
    public IActionResult GetAllUsers()
    {
        // SAFE: now requires authorization
        return Ok(new[] {
            new { id = 1, username = "admin", role = "Admin" },
            new { id = 2, username = "user", role = "User" },
        });
    }

    // ── PASS V8.2.1: [Authorize(Roles = "Admin")] added ────────────────
    // ── FAIL V8.2.2: IDOR — no ownership check ───────────────────────────
    [HttpGet("orders/{id}/details")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetOrderDetailsUnsafe([System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)] int id)
    {
        // BAD: fetches by ID without checking if this user owns the order
        var order = _db.Orders.FirstOrDefault(o => o.Id == id);
        if (order == null)
            return NotFound();

        return Ok(new OrderDto
        {
            Id = order.Id,
            ProductName = order.ProductName,
            Amount = order.Amount,
            Status = order.Status,
        });
    }

    // ── PASS V8.2.1: [Authorize(Roles = "Admin")] added ────────────────
    // ── FAIL V8.2.2 + TRICKY: ownership check uses client-supplied user ID ──
    [HttpGet("orders/by-user/{orderId}")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetOrderByUserSafe([System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)] int orderId, [FromQuery] int userId)
    {
        // Looks like an ownership check, but userId comes from the client!
        var order = _db.Orders
            .FirstOrDefault(o => o.Id == orderId && o.OwnerUserId == userId);

        if (order == null)
            return NotFound();

        // The check passes because userId is attacker-controlled —
        // attacker can set userId to the victim's ID
        return Ok(new OrderDto
        {
            Id = order.Id,
            ProductName = order.ProductName,
            Amount = order.Amount,
            Status = order.Status,
        });
    }
}
