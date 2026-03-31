using LibCafe.Domain.Entities;
using LibCafe.Domain.Interfaces;
using LibCafe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibCafe.Infrastructure.Repositories;

public class BorrowingRepository : IBorrowingRepository
{
    private readonly LibraryCafeDbContext _context;

    public BorrowingRepository(LibraryCafeDbContext context)
    {
        _context = context;
    }

    public async Task<Borrowing?> GetByIdAsync(int id) =>
        await _context.Borrowings
            .Include(b => b.Book)
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Borrowingid == id);

    public async Task<IEnumerable<Borrowing>> GetByUserIdAsync(int userId) =>
        await _context.Borrowings
            .Where(b => b.Userid == userId)
            .Include(b => b.Book)
            .OrderByDescending(b => b.Borrowdate)
            .ToListAsync();

    public async Task<IEnumerable<Borrowing>> GetAllAsync(string? status) =>
        await _context.Borrowings
            .Where(b => status == null || b.Status == status)
            .Include(b => b.Book)
            .Include(b => b.User)
            .OrderByDescending(b => b.Borrowdate)
            .ToListAsync();

    public async Task<IEnumerable<Borrowing>> GetOverdueAsync() =>
        await _context.Borrowings
            .Where(b => b.Status == "borrowed" && b.Duedate < DateTime.Now)
            .Include(b => b.Book)
            .Include(b => b.User)
            .OrderBy(b => b.Duedate)
            .ToListAsync();

    public async Task<bool> HasActiveBorrowingAsync(int userId, int bookId) =>
        await _context.Borrowings
            .AnyAsync(b => b.Userid == userId && b.Bookid == bookId &&
                          (b.Status == "requested" || b.Status == "borrowed"));

    public async Task AddAsync(Borrowing borrowing)
    {
        await _context.Borrowings.AddAsync(borrowing);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Borrowing borrowing)
    {
        _context.Borrowings.Update(borrowing);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var borrowing = await GetByIdAsync(id);
        if (borrowing != null)
        {
            _context.Borrowings.Remove(borrowing);
            await _context.SaveChangesAsync();
        }
    }
}