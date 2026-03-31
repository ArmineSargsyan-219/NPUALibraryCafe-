using LibCafe.Domain.Entities;

namespace LibCafe.Domain.Interfaces;

public interface IFavoriteRepository
{
    Task<IEnumerable<Favorite>> GetByUserIdAsync(string userId);
    Task<IEnumerable<string>> GetIdsByUserAndTypeAsync(string userId, string itemType);
    Task<bool> ExistsAsync(string userId, string itemId, string itemType);
    Task AddAsync(Favorite favorite);
    Task DeleteAsync(string userId, string itemId, string itemType);
}