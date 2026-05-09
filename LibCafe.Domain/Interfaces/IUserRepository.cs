using LibCafe.Domain.Entities;

namespace LibCafe.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task AddAsync(User user);
    Task UpdateNameAsync(int userId, string name);
    Task UpdatePhoneAsync(int userId, string phone);
    Task<string?> GetPhoneAsync(int userId);
    Task UpdatePasswordAsync(int userId, string passwordHash);
}