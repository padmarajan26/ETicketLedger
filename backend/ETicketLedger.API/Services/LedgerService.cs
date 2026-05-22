using ETicketLedger.API.Data;
using ETicketLedger.API.DTOs;
using ETicketLedger.API.Interfaces;
using ETicketLedger.API.Models;
using ETicketLedger.API.Enums;
using Microsoft.EntityFrameworkCore;

namespace ETicketLedger.API.Services;
public class LedgerService : ILedgerService
{
    private readonly AppDbContext _db;
    private readonly ILogger<LedgerService> _logger;
 
    // Chart of accounts (constants keep magic strings in one place)
    private const string CashAccountCode = "1010";
    private const string CashAccountName = "Cash / Payment Gateway";
    private const string RevenueAccountCode = "4010";
    private const string RevenueAccountName = "Ticket Sales Revenue";
 
    public LedgerService(AppDbContext db, ILogger<LedgerService> logger)
    {
        _db = db;
        _logger = logger;
    }
 
    public async Task PostDoubleEntryAsync(Transaction transaction, CancellationToken ct = default)
    {
        var amount = transaction.Amount;
 
        if (amount <= 0)
            throw new InvalidOperationException("Ledger amount must be positive.");
 
        // Build DEBIT and CREDIT entries
        var debit = new LedgerEntry
        {
            TransactionId = transaction.Id,
            AccountCode   = CashAccountCode,
            AccountName   = CashAccountName,
            EntryType     = EntryType.Debit,
            Amount        = amount,
            Description   = $"Payment received via {transaction.PaymentMethod} for Order {transaction.OrderId}",
            PostedAt      = DateTime.UtcNow
        };
 
        var credit = new LedgerEntry
        {
            TransactionId = transaction.Id,
            AccountCode   = RevenueAccountCode,
            AccountName   = RevenueAccountName,
            EntryType     = EntryType.Credit,
            Amount        = amount,
            Description   = $"Revenue recognised for Order {transaction.OrderId}",
            PostedAt      = DateTime.UtcNow
        };
 
        // Invariant check before persisting
        if (debit.Amount != credit.Amount)
            throw new InvalidOperationException(
                $"Ledger imbalance detected: Debit={debit.Amount}, Credit={credit.Amount}");
 
        _db.LedgerEntries.AddRange(debit, credit);
        await _db.SaveChangesAsync(ct);
 
        _logger.LogInformation(
            "Double-entry posted for Transaction {TxId}: Debit {D} / Credit {C}",
            transaction.Id, debit.Amount, credit.Amount);
    }
 
    public async Task<LedgerBalanceDto> GetBalanceAsync(CancellationToken ct = default)
    {
        var entries = await _db.LedgerEntries.ToListAsync(ct);
        var totalDebits  = entries.Where(e => e.EntryType == EntryType.Debit).Sum(e => e.Amount);
        var totalCredits = entries.Where(e => e.EntryType == EntryType.Credit).Sum(e => e.Amount);
 
        return new LedgerBalanceDto { TotalDebits = totalDebits, TotalCredits = totalCredits, IsBalanced = totalDebits == totalCredits };
    }
 
    public async Task<IReadOnlyList<LedgerEntryDto>> GetEntriesAsync(CancellationToken ct = default)
    {
        var entries = await _db.LedgerEntries
            .OrderByDescending(e => e.PostedAt)
            .ToListAsync(ct);
 
        return entries.Select(e => new LedgerEntryDto
        {
            Id = e.Id,
            TransactionId = e.TransactionId,
            AccountCode = e.AccountCode,
            AccountName = e.AccountName,
            EntryType = e.EntryType.ToString(),
            Amount = e.Amount,
            Description = e.Description,
            PostedAt = e.PostedAt
        }).ToList();
    }
}
 