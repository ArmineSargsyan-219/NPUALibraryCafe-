using LibCafe.Domain.Entities;

namespace LibCafe.Domain.Interfaces;

public interface IMenuitemRepository
{
    Task<IEnumerable<Menuitem>> GetAllAvailableAsync();
    Task<IEnumerable<Menuitem>> GetByCategoryAsync(string category);
    Task<IEnumerable<Menuitem>> SearchAsync(string query);
    Task<Menuitem?> GetByIdAsync(string id);
}