using LibCafe.Domain.Entities;
using LibCafe.Domain.Interfaces;
using LibCafe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibCafe.Infrastructure.Repositories;

public class BookRepository : IBookRepository
{
    private readonly LibraryCafeDbContext _context;

    public BookRepository(LibraryCafeDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Book>> GetAllAsync() =>
        await _context.Books.ToListAsync();

    public async Task<Book?> GetByIdAsync(int id) =>
        await _context.Books.FindAsync(id);

    public async Task<IEnumerable<Book>> SearchAsync(string query) =>
        await _context.Books
            .Where(b => b.Title.ToLower().Contains(query.ToLower()) ||
                        b.Author.ToLower().Contains(query.ToLower()))
            .ToListAsync();

    public async Task<IEnumerable<Book>> GetByCategoryAsync(string category) =>
        await _context.Books
            .Where(b => b.Category != null &&
                        b.Category.ToLower() == category.ToLower())
            .ToListAsync();

    public async Task AddAsync(Book book)
    {
        await _context.Books.AddAsync(book);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Book book)
    {
        _context.Books.Update(book);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Bookreview>> GetReviewsAsync(int bookId) =>
        await _context.Bookreviews
            .Where(r => r.Bookid == bookId)
            .Include(r => r.User)
            .ToListAsync();

    public async Task AddReviewAsync(Bookreview review)
    {
        await _context.Bookreviews.AddAsync(review);
        await _context.SaveChangesAsync();
    }
}