using LibCafe.Domain.Entities;

namespace LibCafe.Domain.Interfaces;

public interface IBookRepository
{
    Task<IEnumerable<Book>> GetAllAsync();
    Task<Book?> GetByIdAsync(int id);
    Task<IEnumerable<Book>> SearchAsync(string query);
    Task<IEnumerable<Book>> GetByCategoryAsync(string category);
    Task AddAsync(Book book);
    Task UpdateAsync(Book book);
    Task<IEnumerable<Bookreview>> GetReviewsAsync(int bookId);
    Task AddReviewAsync(Bookreview review);
}