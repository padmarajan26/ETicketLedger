using ETicketLedger.API.Enums;
namespace ETicketLedger.API.Models;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public string PaymentMethod { get; set; } = string.Empty;   // "CreditCard" | "QR"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Audit — no hard deletes
    public bool IsDeleted { get; set; } = false;

    // Navigation
    public Transaction? Transaction { get; set; }
}