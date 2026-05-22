using ETicketLedger.API.Interfaces;
using ETicketLedger.API.Models;

namespace ETicketLedger.API.Handlers;

/// <summary>
/// Simulates a QR-code scan payment.
/// Returns a Pending result immediately; a background task confirms after 8 seconds.
/// </summary>
public class QRHandler : IPaymentHandler
{
    public string PaymentMethod => "QR";

    public Task<PaymentResult> ProcessAsync(PaymentRequest request, CancellationToken ct = default)
    {
        var gatewayRef = $"QR-{Guid.NewGuid():N}".ToUpper()[..20];

        return Task.FromResult(new PaymentResult { 
            IsImmediate= false,     // ← signals caller to stay in Pending state
            GatewayReference= gatewayRef,
            Message= "QR code generated. Awaiting scan confirmation (8 seconds)."
        });
    }
}