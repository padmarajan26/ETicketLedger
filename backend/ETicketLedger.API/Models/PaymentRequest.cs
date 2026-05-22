namespace   ETicketLedger.API.Models;

public class PaymentRequest
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
}
