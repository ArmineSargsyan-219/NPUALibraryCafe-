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

    public async Task UpdatePasswordAsync(int userId, string passwordHash)
    {
        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE users SET password = {0} WHERE id = {1}",
            passwordHash, userId);
    }

    public async Task UpdateAvatarAsync(int userId, string avatarUrl)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null) { user.AvatarUrl = avatarUrl; await _context.SaveChangesAsync(); }
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    => await _context.Users.ToListAsync();

    public async Task DeleteAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user != null)
        {
            user.Fullname = "Deleted User";
            user.Email = $"deleted_{id}@deleted.com";
            user.Passwordhash = Guid.NewGuid().ToString();
            user.Phone = null;
            user.AvatarUrl = null;
            user.Role = "deleted";
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateRoleAsync(int id, string role)
    {
        var user = await _context.Users.FindAsync(id);
        if (user != null) { user.Role = role; await _context.SaveChangesAsync(); }
    }
}