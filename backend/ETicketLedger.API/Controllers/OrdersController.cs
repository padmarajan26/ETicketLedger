using ETicketLedger.API.DTOs;
using ETicketLedger.API.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using ETicketLedger.API.Interfaces;

namespace ETicketLedger.API.Controllers;

/// <summary>
/// Handles the booking/checkout flow and order queries.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IValidator<CheckoutRequest> _validator;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        IOrderService orderService,
        IValidator<CheckoutRequest> validator,
        ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _validator    = validator;
        _logger       = logger;
    }

    /// <summary>Creates a new order and initiates payment.</summary>
    [HttpPost("checkout")]
    [ProducesResponseType(typeof(CheckoutResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        try
        {
            var response = await _orderService.CheckoutAsync(request, ct);
            return CreatedAtAction(nameof(GetOrder), new { id = response.OrderId }, response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("quota"))
        {
            return Conflict(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("concurrent"))
        {
            return Conflict(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Gets a specific order by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder(Guid id, CancellationToken ct)
    {
        var order = await _orderService.GetOrderAsync(id, ct);
        return order is null ? NotFound() : Ok(order);
    }

    /// <summary>Lists all orders (most recent first).</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var orders = await _orderService.GetAllOrdersAsync(ct);
        return Ok(orders);
    }

    /// <summary>Polls the status of a transaction (useful for QR payment flow).</summary>
    [HttpGet("transactions/{transactionId:guid}")]
    [ProducesResponseType(typeof(TransactionStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransactionStatus(Guid transactionId, CancellationToken ct)
    {
        var status = await _orderService.GetTransactionStatusAsync(transactionId, ct);
        return status is null ? NotFound() : Ok(status);
    }
}