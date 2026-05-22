namespace ETicketLedger.API.DTOs;

public class CheckoutRequest
{
    public int TicketId { get; set; }
    public int Quantity { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty; // "CreditCard" | "QR"
}