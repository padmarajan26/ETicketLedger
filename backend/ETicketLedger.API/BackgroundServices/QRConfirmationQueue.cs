using System.Collections.Concurrent;
using ETicketLedger.API.Data;
using ETicketLedger.API.Models;
using ETicketLedger.API.Services;
using Microsoft.EntityFrameworkCore;
using ETicketLedger.API.Interfaces;
using ETicketLedger.API.Enums;

namespace ETicketLedger.API.BackgroundServices;

/// <summary>
/// In-memory queue for pending QR transaction IDs that need background confirmation.
/// </summary>
public class QRConfirmationQueue : IQRConfirmationQueue
{
    private readonly ConcurrentQueue<Guid> _queue = new();

    public void Enqueue(Guid transactionId) => _queue.Enqueue(transactionId);
    public bool TryDequeue(out Guid transactionId) => _queue.TryDequeue(out transactionId);
}

/// <summary>
/// Hosted background worker that polls the QR queue.
/// When a pending transaction is found, it waits 8 seconds, then confirms the
/// transaction, updates the order status, and posts the double-entry ledger.
/// </summary>
public class QRConfirmationWorker : BackgroundService
{
    private readonly IQRConfirmationQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<QRConfirmationWorker> _logger;

    public QRConfirmationWorker(
        IQRConfirmationQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<QRConfirmationWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("QR Confirmation Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_queue.TryDequeue(out var transactionId))
            {
                // Fire-and-forget per transaction so one doesn't block others
                _ = Task.Run(() => ConfirmAfterDelayAsync(transactionId, stoppingToken), stoppingToken);
            }
            else
            {
                await Task.Delay(500, stoppingToken); // poll every 500ms
            }
        }
    }

    private async Task ConfirmAfterDelayAsync(Guid transactionId, CancellationToken ct)
    {
        _logger.LogInformation("QR tx {TxId}: waiting 8 seconds before confirming…", transactionId);
        await Task.Delay(TimeSpan.FromSeconds(8), ct);

        using var scope = _scopeFactory.CreateScope();
        var db     = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ledger = scope.ServiceProvider.GetRequiredService<ILedgerService>();

        var transaction = await db.Transactions
            .Include(t => t.Order)
            .FirstOrDefaultAsync(t => t.Id == transactionId, ct);

        if (transaction is null)
        {
            _logger.LogWarning("QR tx {TxId}: not found in database.", transactionId);
            return;
        }

        if (transaction.Status != TransactionStatus.Pending)
        {
            _logger.LogInformation("QR tx {TxId}: already in status {S}, skipping.", transactionId, transaction.Status);
            return;
        }

        transaction.Status      = TransactionStatus.Completed;
        transaction.CompletedAt = DateTime.UtcNow;
        transaction.Order.Status     = OrderStatus.Confirmed;
        transaction.Order.UpdatedAt  = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await ledger.PostDoubleEntryAsync(transaction, ct);

        _logger.LogInformation("QR tx {TxId}: confirmed and ledger posted.", transactionId);
    }
}