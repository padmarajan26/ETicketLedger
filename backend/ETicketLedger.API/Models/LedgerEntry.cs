using ETicketLedger.API.Enums;
namespace ETicketLedger.API.Models;

public class LedgerEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TransactionId { get; set; }
    public Transaction Transaction { get; set; } = null!;

    /// <summary>AccountCode: e.g. "1010" = Cash/Gateway, "4010" = Sales Revenue</summary>
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;

    public EntryType EntryType { get; set; }
    public decimal Amount { get; set; }

    public string Description { get; set; } = string.Empty;
    public DateTime PostedAt { get; set; } = DateTime.UtcNow;

    // Audit — immutable once posted; no deletes
    public bool IsDeleted { get; set; } = false;
}