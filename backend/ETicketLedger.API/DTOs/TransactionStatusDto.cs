namespace ETicketLedger.API.DTOs;

public class TransactionStatusDto
{
    public Guid TransactionId { get; set; }
    public Guid OrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}