using ETicketLedger.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ETicketLedger.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Ticket ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Ticket>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Name).HasMaxLength(50).IsRequired();
            e.Property(t => t.Price).HasColumnType("decimal(18,2)");
            e.Property(t => t.RemainingQuota).IsConcurrencyToken(); // optimistic lock
            e.HasQueryFilter(t => !t.IsDeleted);
            e.HasIndex(t => t.Name).IsUnique();
        });

        // ── Order ─────────────────────────────────────────────────────────────
        modelBuilder.Entity<Order>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
            e.Property(o => o.UnitPrice).HasColumnType("decimal(18,2)");
            e.Property(o => o.CustomerEmail).HasMaxLength(200).IsRequired();
            e.Property(o => o.CustomerName).HasMaxLength(200).IsRequired();
            e.Property(o => o.PaymentMethod).HasMaxLength(50).IsRequired();
            e.HasQueryFilter(o => !o.IsDeleted);

            e.HasOne(o => o.Ticket)
             .WithMany(t => t.Orders)
             .HasForeignKey(o => o.TicketId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Transaction ───────────────────────────────────────────────────────
        modelBuilder.Entity<Transaction>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Amount).HasColumnType("decimal(18,2)");
            e.Property(t => t.PaymentMethod).HasMaxLength(50).IsRequired();
            e.HasQueryFilter(t => !t.IsDeleted);

            e.HasOne(t => t.Order)
             .WithOne(o => o.Transaction)
             .HasForeignKey<Transaction>(t => t.OrderId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── LedgerEntry ───────────────────────────────────────────────────────
        modelBuilder.Entity<LedgerEntry>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.Amount).HasColumnType("decimal(18,2)");
            e.Property(l => l.AccountCode).HasMaxLength(20).IsRequired();
            e.Property(l => l.AccountName).HasMaxLength(100).IsRequired();
            e.Property(l => l.Description).HasMaxLength(500);
            e.HasQueryFilter(l => !l.IsDeleted);

            e.HasOne(l => l.Transaction)
             .WithMany(t => t.LedgerEntries)
             .HasForeignKey(l => l.TransactionId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Seed Data ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Ticket>().HasData(
            new Ticket
            {
                Id = 1, Name = "Gold", Description = "Gold tier ticket with standard access",
                Price = 100m, TotalQuota = 100, RemainingQuota = 100, IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1), UpdatedAt = new DateTime(2026, 1, 1),
                RowVersion = new byte[8]
            },
            new Ticket
            {
                Id = 2, Name = "Premium", Description = "Premium tier ticket with lounge access",
                Price = 200m, TotalQuota = 50, RemainingQuota = 50, IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1), UpdatedAt = new DateTime(2026, 1, 1),
                RowVersion = new byte[8]
            },
            new Ticket
            {
                Id = 3, Name = "VIP", Description = "VIP tier with exclusive backstage access",
                Price = 500m, TotalQuota = 20, RemainingQuota = 20, IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1), UpdatedAt = new DateTime(2026, 1, 1),
                RowVersion = new byte[8]
            }
        );
    }
}