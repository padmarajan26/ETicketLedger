using ETicketLedger.API.Models;
namespace ETicketLedger.API.Interfaces;

/// <summary>
/// Strategy interface for pluggable payment methods.
/// Each implementation encapsulates its own processing logic.
/// </summary>
public interface IPaymentHandler
{
    /// <summary>Unique key used to select this handler (e.g. "CreditCard", "QR")</summary>
    string PaymentMethod { get; }

    /// <summary>
    /// Process the payment for the given order.
    /// Returns a gateway reference string on success.
    /// Throws PaymentException on failure.
    /// </summary>
    Task<PaymentResult> ProcessAsync(PaymentRequest request, CancellationToken ct = default);
}
