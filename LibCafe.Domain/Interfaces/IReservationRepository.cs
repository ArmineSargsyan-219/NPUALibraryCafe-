using LibCafe.Domain.Entities;

namespace LibCafe.Domain.Interfaces;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(int id);
    Task<IEnumerable<Reservation>> GetByUserEmailAsync(string email);
    Task<IEnumerable<Reservation>> GetAllAsync();
    Task<IEnumerable<CafeTable>> GetAllTablesAsync();
    Task<IEnumerable<int>> GetReservedTableIdsAsync(DateTime startTime, DateTime endTime);
    Task<bool> HasConflictAsync(int tableId, DateTime startTime, DateTime endTime);
    Task<bool> HasOtherActiveReservationsAsync(int tableId, int excludeId);
    Task<IEnumerable<Reservation>> GetUpcomingAsync(DateTime from, DateTime to);
    Task<IEnumerable<Reservation>> GetExpiredAsync(DateTime now);
    Task AddAsync(Reservation reservation);
    Task UpdateAsync(Reservation reservation);
}