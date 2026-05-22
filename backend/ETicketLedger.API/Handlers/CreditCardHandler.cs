using ETicketLedger.API.Interfaces;
using ETicketLedger.API.Models;
using ETicketLedger.API.DTOs;
namespace ETicketLedger.API.Handlers;

/// <summary>
/// Simulates a credit card payment — instant approval.
/// </summary>
public class CreditCardHandler : IPaymentHandler
{
    public string PaymentMethod => "CreditCard";

    public Task<PaymentResult> ProcessAsync(PaymentRequest request, CancellationToken ct = default)
    {
        // Simulate gateway call (instant)
        var gatewayRef = $"CC-{Guid.NewGuid():N}".ToUpper()[..20];

        return Task.FromResult(new PaymentResult{
            IsImmediate= true,
            GatewayReference= gatewayRef,
            Message= "Credit card payment approved instantly."
        });
    }
}