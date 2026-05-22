namespace ETicketLedger.API.Models;

public class PaymentResult
{
    public bool IsImmediate { get; set; }   // false = QR pending state
    public string GatewayReference { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}