namespace ETicketLedger.API.DTOs;
public class CheckoutResponse
{
    public Guid OrderId { get; set; }
    public Guid TransactionId { get; set; }
    public string Status { get; set; } = string.Empty; // "Confirmed" | "Pending"
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Message { get; set; } = string.Empty;
}