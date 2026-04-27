using LibCafe.Domain.Entities;
using LibCafe.Domain.Interfaces;
using LibCafe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibCafe.Infrastructure.Repositories;

public class CafeorderRepository : ICafeorderRepository
{
    private readonly LibraryCafeDbContext _context;

    public CafeorderRepository(LibraryCafeDbContext context)
    {
        _context = context;
    }

    public async Task<Cafeorder?> GetByIdAsync(int id) =>
        await _context.Cafeorders
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Orderid == id);

    public async Task<IEnumerable<Cafeorder>> GetByUserIdAsync(int userId) =>
        await _context.Cafeorders
            .Where(o => o.Userid == userId)
            .OrderByDescending(o => o.Orderdate)
            .ToListAsync();

    public async Task<IEnumerable<Cafeorder>> GetPendingAsync() =>
        await _context.Cafeorders
            .Where(o => o.Status == "pending" || o.Status == "ready" || o.Status == "history")
            .Include(o => o.User)
            .OrderBy(o => o.Orderdate)
            .ToListAsync();

    public async Task<IEnumerable<Cafeorder>> GetAllAsync() =>
        await _context.Cafeorders
            .Include(o => o.User)
            .OrderByDescending(o => o.Orderdate)
            .ToListAsync();

    public async Task<int> AddAsync(Cafeorder order)
    {
        await _context.Cafeorders.AddAsync(order);
        await _context.SaveChangesAsync();
        return order.Orderid;
    }

    public async Task UpdateAsync(Cafeorder order)
    {
        _context.Cafeorders.Update(order);
        await _context.SaveChangesAsync();
    }
}