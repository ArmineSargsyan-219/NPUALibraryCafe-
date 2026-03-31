using LibCafe.Domain.Entities;
using LibCafe.Domain.Interfaces;
using LibCafe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibCafe.Infrastructure.Repositories;

public class FavoriteRepository : IFavoriteRepository
{
    private readonly LibraryCafeDbContext _context;

    public FavoriteRepository(LibraryCafeDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Favorite>> GetByUserIdAsync(string userId) =>
        await _context.Favorites
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

    public async Task<IEnumerable<string>> GetIdsByUserAndTypeAsync(string userId, string itemType) =>
        await _context.Favorites
            .Where(f => f.UserId == userId && f.ItemType == itemType)
            .Select(f => f.ItemId)
            .ToListAsync();

    public async Task<bool> ExistsAsync(string userId, string itemId, string itemType) =>
        await _context.Favorites
            .AnyAsync(f => f.UserId == userId && f.ItemId == itemId && f.ItemType == itemType);

    public async Task AddAsync(Favorite favorite)
    {
        await _context.Favorites.AddAsync(favorite);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string userId, string itemId, string itemType)
    {
        var favorite = await _context.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.ItemId == itemId && f.ItemType == itemType);
        if (favorite != null)
        {
            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();
        }
    }
}