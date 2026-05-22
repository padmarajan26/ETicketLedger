using ETicketLedger.API.Enums;
namespace ETicketLedger.API.Models;

public class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
    public string? GatewayReference { get; set; }    // Simulated gateway ref
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    // Audit
    public bool IsDeleted { get; set; } = false;

    // Navigation
    public ICollection<LedgerEntry> LedgerEntries { get; set; } = new List<LedgerEntry>();
}