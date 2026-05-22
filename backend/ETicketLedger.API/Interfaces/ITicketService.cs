using ETicketLedger.API.DTOs;
namespace ETicketLedger.API.Interfaces;
public interface ITicketService
{
    Task<IReadOnlyList<TicketDto>> GetAllAsync(CancellationToken ct = default);
    Task<TicketDto?> GetByIdAsync(int id, CancellationToken ct = default);
}