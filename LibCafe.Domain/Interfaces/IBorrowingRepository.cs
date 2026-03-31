using LibCafe.Domain.Entities;

namespace LibCafe.Domain.Interfaces;

public interface IBorrowingRepository
{
    Task<Borrowing?> GetByIdAsync(int id);
    Task<IEnumerable<Borrowing>> GetByUserIdAsync(int userId);
    Task<IEnumerable<Borrowing>> GetAllAsync(string? status);
    Task<IEnumerable<Borrowing>> GetOverdueAsync();
    Task<bool> HasActiveBorrowingAsync(int userId, int bookId);
    Task AddAsync(Borrowing borrowing);
    Task UpdateAsync(Borrowing borrowing);
    Task DeleteAsync(int id);
}