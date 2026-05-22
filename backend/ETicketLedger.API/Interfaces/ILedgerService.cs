using ETicketLedger.API.Models;
using  ETicketLedger.API.DTOs;
public interface ILedgerService
{
    /// <summary>
    /// Posts a balanced double-entry pair for a completed transaction.
    /// Debit  1010 Cash/Gateway  ←→  Credit 4010 Sales Revenue
    /// Throws InvalidOperationException if entries would not balance.
    /// </summary>
    Task PostDoubleEntryAsync(Transaction transaction, CancellationToken ct = default);
 
    Task<LedgerBalanceDto> GetBalanceAsync(CancellationToken ct = default);
    Task<IReadOnlyList<LedgerEntryDto>> GetEntriesAsync(CancellationToken ct = default);
}