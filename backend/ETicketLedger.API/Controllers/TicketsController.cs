using ETicketLedger.API.Services;
using Microsoft.AspNetCore.Mvc;
using ETicketLedger.API.Interfaces;

namespace ETicketLedger.API.Controllers;

/// <summary>
/// Manages ticket catalogue and availability.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;

    public TicketsController(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    /// <summary>Returns all active ticket types with their current quota.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var tickets = await _ticketService.GetAllAsync(ct);
        return Ok(tickets);
    }

    /// <summary>Returns a single ticket by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var ticket = await _ticketService.GetByIdAsync(id, ct);
        return ticket is null ? NotFound() : Ok(ticket);
    }
}