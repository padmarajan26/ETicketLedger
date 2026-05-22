using ETicketLedger.API.DTOs;
using FluentValidation;

namespace ETicketLedger.API.Validators;

public class CheckoutRequestValidator : AbstractValidator<CheckoutRequest>
{
    private static readonly string[] AllowedPaymentMethods = { "CreditCard", "QR" };

    public CheckoutRequestValidator()
    {
        RuleFor(x => x.TicketId)
            .GreaterThan(0).WithMessage("TicketId must be a positive integer.");

        RuleFor(x => x.Quantity)
            .InclusiveBetween(1, 10).WithMessage("Quantity must be between 1 and 10.");

        RuleFor(x => x.CustomerName)
            .NotEmpty().WithMessage("Customer name is required.")
            .MaximumLength(200);

        RuleFor(x => x.CustomerEmail)
            .NotEmpty().WithMessage("Customer email is required.")
            .Matches(@"^[^@\s]+@[^@\s]+\.[^@\s]+$").WithMessage("A valid email address is required.")
            .MaximumLength(200);

        RuleFor(x => x.PaymentMethod)
            .NotEmpty()
            .Must(m => AllowedPaymentMethods.Contains(m, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"PaymentMethod must be one of: {string.Join(", ", AllowedPaymentMethods)}.");
    }
}