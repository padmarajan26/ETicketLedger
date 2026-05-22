using ETicketLedger.API.Data;
using ETicketLedger.API.DTOs;
using Microsoft.EntityFrameworkCore;
using ETicketLedger.API.Interfaces;
using ETicketLedger.API.Models;

namespace ETicketLedger.API.Services;
public class TicketService : ITicketService
{
    private readonly AppDbContext _db;

    public TicketService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<TicketDto>> GetAllAsync(CancellationToken ct = default)
    {
        var tickets = await _db.Tickets
            .Where(t => t.IsActive)
            .OrderBy(t => t.Price)
            .ToListAsync(ct);

        return tickets.Select(t => new TicketDto {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            Price = t.Price,
            TotalQuota = t.TotalQuota,
            RemainingQuota = t.RemainingQuota,
            IsActive = t.IsActive
        }).ToList();
    }

    public async Task<TicketDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var t = await _db.Tickets.FirstOrDefaultAsync(x => x.Id == id && x.IsActive, ct);
        if (t is null) return null;
        return new TicketDto{ Id = t.Id, Name = t.Name, Description = t.Description, Price = t.Price, TotalQuota = t.TotalQuota, RemainingQuota = t.RemainingQuota, IsActive = t.IsActive };
    }
}