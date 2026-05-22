using ETicketLedger.API.Interfaces;
namespace ETicketLedger.API.Handlers;

/// <summary>
/// Resolves the correct IPaymentHandler for a given payment method string.
/// New payment methods are added by registering additional IPaymentHandler implementations — 
/// no changes needed here (Open/Closed Principle).
/// </summary>

public class PaymentHandlerFactory : IPaymentHandlerFactory
{
    private readonly IEnumerable<IPaymentHandler> _handlers;
 
    public PaymentHandlerFactory(IEnumerable<IPaymentHandler> handlers)
    {
        _handlers = handlers;
    }
 
    public IPaymentHandler Resolve(string paymentMethod)
    {
        var handler = _handlers.FirstOrDefault(h =>
            h.PaymentMethod.Equals(paymentMethod, StringComparison.OrdinalIgnoreCase));
 
        if (handler is null)
            throw new ArgumentException($"No payment handler registered for method '{paymentMethod}'.");
 
        return handler;
    }
}