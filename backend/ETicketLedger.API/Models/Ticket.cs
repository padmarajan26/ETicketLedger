namespace ETicketLedger.API.Models;

public class Ticket
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;         // Gold, Premium, VIP
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int TotalQuota { get; set; }
    public int RemainingQuota { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Soft-delete / audit — never hard-delete
    public bool IsDeleted { get; set; } = false;

    // EF navigation
    public ICollection<Order> Orders { get; set; } = new List<Order>();

    // Optimistic-concurrency row version (handles race conditions)
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}