using LibCafe.Domain.Entities;
using LibCafe.Domain.Interfaces;
using LibCafe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibCafe.Infrastructure.Repositories;

public class MenuitemRepository : IMenuitemRepository
{
    private readonly LibraryCafeDbContext _context;

    public MenuitemRepository(LibraryCafeDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Menuitem>> GetAllAvailableAsync() =>
        await _context.Menuitems
            .Where(m => m.Available)
            .OrderBy(m => m.CategoryId)
            .ThenBy(m => m.Itemname)
            .ToListAsync();

    public async Task<IEnumerable<Menuitem>> GetByCategoryAsync(string category) =>
        await _context.Menuitems
            .Where(m => m.Available && m.CategoryId == category)
            .OrderBy(m => m.Itemname)
            .ToListAsync();

    public async Task<IEnumerable<Menuitem>> SearchAsync(string query) =>
        await _context.Menuitems
            .Where(m => m.Available && (
                m.Itemname.ToLower().Contains(query.ToLower()) ||
                (m.Description != null && m.Description.ToLower().Contains(query.ToLower()))))
            .OrderBy(m => m.Itemname)
            .ToListAsync();

    public async Task<Menuitem?> GetByIdAsync(string id) =>
        await _context.Menuitems.FindAsync(id);
}