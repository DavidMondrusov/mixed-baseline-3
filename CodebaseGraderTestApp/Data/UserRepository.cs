using Microsoft.EntityFrameworkCore;
using CodebaseGraderTestApp.Models;

namespace CodebaseGraderTestApp.Data;

public class UserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db) => _db = db;

    // ── PASS: query filtered by userId ──────────────────────────────────
    public async Task<List<OrderEntity>> GetOrdersForUser(int userId)
    {
        return await _db.Orders
            .Where(o => o.OwnerUserId == userId)
            .ToListAsync();
    }

    // ── FAIL V8.2.2: query by ID only, no ownership check ────────────────
    public async Task<OrderEntity?> GetOrderByIdUnsafe(int orderId)
    {
        return await _db.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    // ── FAIL: query with user ID from caller (not validated) ─────────────
    public async Task<OrderEntity?> GetOrderForUserTricky(int orderId, int claimedUserId)
    {
        return await _db.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId && o.OwnerUserId == claimedUserId);
    }
}
