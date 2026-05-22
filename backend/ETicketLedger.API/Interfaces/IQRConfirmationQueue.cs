namespace ETicketLedger.API.Interfaces;
public interface IQRConfirmationQueue
{
    void Enqueue(Guid transactionId);
    bool TryDequeue(out Guid transactionId);
}