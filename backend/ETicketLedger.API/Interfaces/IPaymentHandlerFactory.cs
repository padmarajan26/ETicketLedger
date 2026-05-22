using ETicketLedger.API.Interfaces;
namespace ETicketLedger.API.Interfaces;
public interface IPaymentHandlerFactory
{
    IPaymentHandler Resolve(string paymentMethod);
}