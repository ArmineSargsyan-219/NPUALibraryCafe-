using LibCafe.Domain.Entities;
using LibCafe.Domain.Interfaces;
using LibCafe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibCafe.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly LibraryCafeDbContext _context;

    public UserRepository(LibraryCafeDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(int id) =>
        await _context.Users.FindAsync(id);

    public async Task<User?> GetByEmailAsync(string email) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateNameAsync(int userId, string name)
    {
        var user = await GetByIdAsync(userId);
        if (user == null) return;
        user.Fullname = name;
        await _context.SaveChangesAsync();
    }

    public async Task UpdatePhoneAsync(int userId, string phone)
    {
        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE users SET phone = {0} WHERE id = {1}", phone, userId);
    }

    public async Task<string?> GetPhoneAsync(int userId)
    {
        var result = await _context.Database
            .SqlQueryRaw<string>("SELECT phone FROM users WHERE id = {0}", userId)
            .ToListAsync();
        return result.FirstOrDefault();
    }
}