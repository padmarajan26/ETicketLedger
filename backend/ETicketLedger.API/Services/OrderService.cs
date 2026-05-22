using ETicketLedger.API.BackgroundServices;
using ETicketLedger.API.Data;
using ETicketLedger.API.DTOs;
using ETicketLedger.API.Handlers;
using ETicketLedger.API.Models;
using ETicketLedger.API.Interfaces;
using Microsoft.EntityFrameworkCore;
using ETicketLedger.API.Enums;

namespace ETicketLedger.API.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;
    private readonly IPaymentHandlerFactory _paymentFactory;
    private readonly ILedgerService _ledger;
    private readonly IQRConfirmationQueue _qrQueue;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        AppDbContext db,
        IPaymentHandlerFactory paymentFactory,
        ILedgerService ledger,
        IQRConfirmationQueue qrQueue,
        ILogger<OrderService> logger)
    {
        _db = db;
        _paymentFactory = paymentFactory;
        _ledger = ledger;
        _qrQueue = qrQueue;
        _logger = logger;
    }

    public async Task<CheckoutResponse> CheckoutAsync(CheckoutRequest request, CancellationToken ct = default)
    {
        // ── 1. Validate ticket & quota (with concurrency protection) ──────────
        var ticket = await _db.Tickets
            .FirstOrDefaultAsync(t => t.Id == request.TicketId && t.IsActive, ct)
            ?? throw new InvalidOperationException($"Ticket {request.TicketId} not found or inactive.");

        if (ticket.RemainingQuota < request.Quantity)
            throw new InvalidOperationException(
                $"Insufficient quota. Requested: {request.Quantity}, Available: {ticket.RemainingQuota}.");

        // ── 2. Create Order ───────────────────────────────────────────────────
        var order = new Order
        {
            TicketId      = ticket.Id,
            Quantity      = request.Quantity,
            UnitPrice     = ticket.Price,
            TotalAmount   = ticket.Price * request.Quantity,
            CustomerEmail = request.CustomerEmail,
            CustomerName  = request.CustomerName,
            PaymentMethod = request.PaymentMethod,
            Status        = OrderStatus.Pending
        };
        _db.Orders.Add(order);

        // ── 3. Decrement quota atomically (optimistic concurrency via EF token) 
        ticket.RemainingQuota -= request.Quantity;
        ticket.UpdatedAt = DateTime.UtcNow;

        // ── 4. Resolve payment handler & process ──────────────────────────────
        var handler = _paymentFactory.Resolve(request.PaymentMethod);
        var paymentResult = await handler.ProcessAsync(new PaymentRequest {
            OrderId = order.Id,
            Amount = order.TotalAmount,
            CustomerEmail = order.CustomerEmail,
            CustomerName = order.CustomerName
        }, ct);

        // ── 5. Create Transaction ─────────────────────────────────────────────
        var transaction = new Transaction
        {
            OrderId          = order.Id,
            Amount           = order.TotalAmount,
            PaymentMethod    = request.PaymentMethod,
            GatewayReference = paymentResult.GatewayReference,
            Status           = paymentResult.IsImmediate
                                   ? TransactionStatus.Completed
                                   : TransactionStatus.Pending,
            CompletedAt      = paymentResult.IsImmediate ? DateTime.UtcNow : null
        };
        _db.Transactions.Add(transaction);
        order.Status = paymentResult.IsImmediate ? OrderStatus.Confirmed : OrderStatus.Pending;

        // ── 6. Save everything in one DB round-trip ───────────────────────────
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Race condition: another request modified RemainingQuota simultaneously
            throw new InvalidOperationException(
                "Ticket quota was modified by a concurrent request. Please retry.");
        }

        // ── 7. Post ledger entries (if immediate) ─────────────────────────────
        if (paymentResult.IsImmediate)
        {
            await _ledger.PostDoubleEntryAsync(transaction, ct);
            _logger.LogInformation("Order {OrderId} confirmed immediately via CreditCard.", order.Id);
        }
        else
        {
            // Enqueue the QR background confirmation (8-second delay)
            _qrQueue.Enqueue(transaction.Id);
            _logger.LogInformation("Order {OrderId} queued for QR confirmation.", order.Id);
        }

        return new CheckoutResponse { 
            OrderId       = order.Id,
            TransactionId = transaction.Id,
            Status        = paymentResult.IsImmediate ? "Confirmed" : "Pending",
            TotalAmount   = order.TotalAmount,
            PaymentMethod = request.PaymentMethod,
            Timestamp     = order.CreatedAt,
            Message       = paymentResult.Message
        };
    }

    public async Task<OrderDto?> GetOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .Include(o => o.Ticket)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        return order is null ? null : MapToDto(order);
    }

    public async Task<IReadOnlyList<OrderDto>> GetAllOrdersAsync(CancellationToken ct = default)
    {
        var orders = await _db.Orders
            .Include(o => o.Ticket)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

        return orders.Select(MapToDto).ToList();
    }

    public async Task<TransactionStatusDto?> GetTransactionStatusAsync(Guid transactionId, CancellationToken ct = default)
    {
        var tx = await _db.Transactions.FirstOrDefaultAsync(t => t.Id == transactionId, ct);
        if (tx is null) return null;

        return new TransactionStatusDto
        {
            TransactionId = tx.Id,
            OrderId = tx.OrderId,
            Status = tx.Status.ToString(),
            Amount = tx.Amount,
            PaymentMethod = tx.PaymentMethod,
            CreatedAt = tx.CreatedAt,
            CompletedAt = tx.CompletedAt
        };
    }

    private static OrderDto MapToDto(Order o) => new OrderDto
    {
        Id = o.Id,
        TicketId = o.TicketId,
        TicketName = o.Ticket?.Name ?? string.Empty,
        Quantity = o.Quantity,
        UnitPrice = o.UnitPrice,
        TotalAmount = o.TotalAmount,
        CustomerEmail = o.CustomerEmail,
        CustomerName = o.CustomerName,
        Status = o.Status.ToString(),
        PaymentMethod = o.PaymentMethod,
        CreatedAt = o.CreatedAt
    };
}