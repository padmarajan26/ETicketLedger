namespace ETicketLedger.API.DTOs;

public class LedgerEntryDto
{
    public Guid Id { get; set; }
    public Guid TransactionId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string EntryType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime PostedAt { get; set; }
}