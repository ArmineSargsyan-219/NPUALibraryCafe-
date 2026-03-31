using LibCafe.Domain.Entities;

namespace LibCafe.Domain.Interfaces;

public interface ICafeorderRepository
{
    Task<Cafeorder?> GetByIdAsync(int id);
    Task<IEnumerable<Cafeorder>> GetByUserIdAsync(int userId);
    Task<IEnumerable<Cafeorder>> GetPendingAsync();
    Task<IEnumerable<Cafeorder>> GetAllAsync();
    Task<int> AddAsync(Cafeorder order);
    Task UpdateAsync(Cafeorder order);
}