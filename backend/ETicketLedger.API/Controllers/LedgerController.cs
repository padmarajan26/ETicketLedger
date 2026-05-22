using ETicketLedger.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace ETicketLedger.API.Controllers;

/// <summary>
/// Exposes the double-entry ledger for audit and balance verification.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class LedgerController : ControllerBase
{
    private readonly ILedgerService _ledger;

    public LedgerController(ILedgerService ledger) => _ledger = ledger;

    /// <summary>
    /// Returns all ledger entries ordered by posting date (most recent first).
    /// </summary>
    [HttpGet("entries")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEntries(CancellationToken ct)
    {
        var entries = await _ledger.GetEntriesAsync(ct);
        return Ok(entries);
    }

    /// <summary>
    /// Returns the overall ledger balance.
    /// IsBalanced must always be true — if false, data integrity is compromised.
    /// </summary>
    [HttpGet("balance")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBalance(CancellationToken ct)
    {
        var balance = await _ledger.GetBalanceAsync(ct);
        return Ok(balance);
    }
}