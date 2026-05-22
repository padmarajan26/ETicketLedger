using ETicketLedger.API.DTOs;
using ETicketLedger.API.Validators;
using FluentAssertions;
using Xunit;

namespace ETicketLedger.Tests;

public class CheckoutValidatorTests
{
	private readonly CheckoutRequestValidator _validator = new();

	private static CheckoutRequest Valid() => new CheckoutRequest {
		TicketId = 1,
		Quantity = 2,
		CustomerName = "John Doe",
		CustomerEmail = "john@example.com",
		PaymentMethod = "CreditCard"
	};

	[Fact]
	public async Task Valid_Request_Passes()
	{
		var result = await _validator.ValidateAsync(Valid());
		result.IsValid.Should().BeTrue();
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public async Task Invalid_TicketId_Fails(int ticketId)
	{
		var result = await _validator.ValidateAsync(new CheckoutRequest { TicketId = ticketId, CustomerEmail="john@example.com", CustomerName="John Doe", PaymentMethod="CreditCard", Quantity = 2 });
		result.IsValid.Should().BeFalse();
	}

	[Theory]
	[InlineData(0)]
	[InlineData(11)]
	public async Task Invalid_Quantity_Fails(int qty)
	{
		var result = await _validator.ValidateAsync(new CheckoutRequest { TicketId = 1, CustomerEmail="jane@gmail.com", CustomerName="Jane Smith", PaymentMethod="QR", Quantity = qty });
		result.IsValid.Should().BeFalse();
	}

	[Theory]
	[InlineData("")]
	[InlineData("notanemail")]
	public async Task Invalid_Email_Fails(string email)
	{
		var result = await _validator.ValidateAsync(new CheckoutRequest { TicketId = 1, CustomerEmail = email, CustomerName = "John Doe", PaymentMethod = "CreditCard", Quantity = 2 });
		result.IsValid.Should().BeFalse();
	}

	[Theory]
	[InlineData("Bitcoin")]
	[InlineData("Cash")]
	[InlineData("")]
	public async Task Invalid_PaymentMethod_Fails(string method)
	{
		var result = await _validator.ValidateAsync(new CheckoutRequest { TicketId = 1, CustomerEmail="john@example.com", CustomerName="John Doe", PaymentMethod = method, Quantity = 2 });
		result.IsValid.Should().BeFalse();
	}

	[Theory]
	[InlineData("CreditCard")]
	[InlineData("QR")]
	[InlineData("creditcard")]  // case-insensitive
	public async Task Valid_PaymentMethods_Pass(string method)
	{
		var result = await _validator.ValidateAsync(new CheckoutRequest { TicketId = 1, CustomerEmail="john@example.com", CustomerName="John Doe", PaymentMethod = method, Quantity = 2 });
		result.IsValid.Should().BeTrue();
	}
}
