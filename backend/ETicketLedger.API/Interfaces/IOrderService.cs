using ETicketLedger.API.DTOs;
using ETicketLedger.API.Interfaces;

namespace ETicketLedger.API.Interfaces;
public interface IOrderService
{
    Task<CheckoutResponse> CheckoutAsync(CheckoutRequest request, CancellationToken ct = default);
    Task<OrderDto?> GetOrderAsync(Guid orderId, CancellationToken ct = default);
    Task<IReadOnlyList<OrderDto>> GetAllOrdersAsync(CancellationToken ct = default);
    Task<TransactionStatusDto?> GetTransactionStatusAsync(Guid transactionId, CancellationToken ct = default);
}
 