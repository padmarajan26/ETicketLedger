namespace ETicketLedger.API.DTOs;

public class LedgerBalanceDto
{
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
    public bool IsBalanced { get; set; }
}