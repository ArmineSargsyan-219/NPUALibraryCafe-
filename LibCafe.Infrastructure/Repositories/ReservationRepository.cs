using LibCafe.Domain.Entities;
using LibCafe.Domain.Interfaces;
using LibCafe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibCafe.Infrastructure.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly LibraryCafeDbContext _context;

    public ReservationRepository(LibraryCafeDbContext context)
    {
        _context = context;
    }

    public async Task<Reservation?> GetByIdAsync(int id) =>
        await _context.Reservations
            .Include(r => r.Table)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<IEnumerable<Reservation>> GetByUserEmailAsync(string email) =>
        await _context.Reservations
            .Include(r => r.Table)
            .Where(r => r.UserEmail == email)
            .OrderByDescending(r => r.StartTime)
            .ToListAsync();

    public async Task<IEnumerable<Reservation>> GetAllAsync() =>
        await _context.Reservations
            .Include(r => r.Table)
            .OrderByDescending(r => r.StartTime)
            .ToListAsync();

    public async Task<IEnumerable<CafeTable>> GetAllTablesAsync() =>
        await _context.CafeTables.ToListAsync();

    public async Task<IEnumerable<int>> GetReservedTableIdsAsync(DateTime startTime, DateTime endTime) =>
        await _context.Reservations
            .Where(r =>
                r.Status != "Cancelled" && r.Status != "Expired" &&
                r.StartTime < endTime && r.EndTime > startTime)
            .Select(r => r.TableId)
            .ToListAsync();

    public async Task<bool> HasConflictAsync(int tableId, DateTime startTime, DateTime endTime) =>
        await _context.Reservations
            .AnyAsync(r =>
                r.TableId == tableId &&
                r.Status != "Cancelled" && r.Status != "Expired" &&
                r.StartTime < endTime && r.EndTime > startTime);

    public async Task<bool> HasOtherActiveReservationsAsync(int tableId, int excludeId) =>
        await _context.Reservations
            .AnyAsync(r =>
                r.TableId == tableId && r.Id != excludeId &&
                r.Status != "Cancelled" && r.Status != "Expired");

    public async Task<IEnumerable<Reservation>> GetUpcomingAsync(DateTime from, DateTime to) =>
        await _context.Reservations
            .Include(r => r.Table)
            .Where(r => r.Status == "Active" && r.StartTime <= to && r.StartTime > from)
            .ToListAsync();

    public async Task<IEnumerable<Reservation>> GetExpiredAsync(DateTime now) =>
        await _context.Reservations
            .Include(r => r.Table)
            .Where(r => (r.Status == "Active" || r.Status == "Confirmed") && r.EndTime <= now)
            .ToListAsync();

    public async Task AddAsync(Reservation reservation)
    {
        await _context.Reservations.AddAsync(reservation);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Reservation reservation)
    {
        _context.Reservations.Update(reservation);
        await _context.SaveChangesAsync();
    }
}