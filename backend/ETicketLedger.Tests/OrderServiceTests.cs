using ETicketLedger.API.Data;
using ETicketLedger.API.DTOs;
using ETicketLedger.API.Enums;
using ETicketLedger.API.Handlers;
using ETicketLedger.API.Interfaces;
using ETicketLedger.API.Models;
using ETicketLedger.API.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ETicketLedger.Tests;

public class OrderServiceTests
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<IPaymentHandlerFactory> _paymentFactoryMock;
    private readonly Mock<ILedgerService> _ledgerServiceMock;
    private readonly Mock<IQRConfirmationQueue> _qrQueueMock;
    private readonly Mock<ILogger<OrderService>> _loggerMock;
    private readonly OrderService _service;

    public OrderServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _paymentFactoryMock = new Mock<IPaymentHandlerFactory>();
        _ledgerServiceMock = new Mock<ILedgerService>();
        _qrQueueMock = new Mock<IQRConfirmationQueue>();
        _loggerMock = new Mock<ILogger<OrderService>>();

        _service = new OrderService(
            _dbContext,
            _paymentFactoryMock.Object,
            _ledgerServiceMock.Object,
            _qrQueueMock.Object,
            _loggerMock.Object);
    }

    private void SeedTicket(int id = 1, decimal price = 100m, int quota = 50)
    {
        var ticket = new Ticket
        {
            Id = id,
            Name = $"Ticket {id}",
            Description = "Test ticket",
            Price = price,
            TotalQuota = quota,
            RemainingQuota = quota,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RowVersion = new byte[8],
            IsDeleted = false
        };
        _dbContext.Tickets.Add(ticket);
        _dbContext.SaveChanges();
    }

    private CheckoutRequest CreateValidCheckoutRequest(int ticketId = 1, int quantity = 2)
    {
        return new CheckoutRequest
        {
            TicketId = ticketId,
            Quantity = quantity,
            CustomerName = "John Doe",
            CustomerEmail = "john@example.com",
            PaymentMethod = "CreditCard"
        };
    }

    private Mock<IPaymentHandler> MockPaymentHandler(bool isImmediate = true, string message = "Success")
    {
        var handlerMock = new Mock<IPaymentHandler>();
        handlerMock.Setup(h => h.ProcessAsync(It.IsAny<PaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentResult
            {
                IsImmediate = isImmediate,
                GatewayReference = Guid.NewGuid().ToString(),
                Message = message
            });
        return handlerMock;
    }

    [Fact]
    public async Task CheckoutAsync_WithValidRequest_CreatesOrderAndTransaction()
    {
        // Arrange
        SeedTicket(1, 100m, 50);
        var request = CreateValidCheckoutRequest();
        var handlerMock = MockPaymentHandler();
        _paymentFactoryMock.Setup(f => f.Resolve(request.PaymentMethod)).Returns(handlerMock.Object);
        var ct = CancellationToken.None;

        // Act
        var response = await _service.CheckoutAsync(request, ct);

        // Assert
        response.Should().NotBeNull();
        response.OrderId.Should().NotBeEmpty();
        response.TransactionId.Should().NotBeEmpty();
        response.Status.Should().Be("Confirmed");
        response.TotalAmount.Should().Be(200m);

        var order = await _dbContext.Orders.FirstAsync();
        order.Quantity.Should().Be(2);
        order.CustomerEmail.Should().Be("john@example.com");

        var transaction = await _dbContext.Transactions.FirstAsync();
        transaction.Amount.Should().Be(200m);
    }

    [Fact]
    public async Task CheckoutAsync_WithInvalidTicketId_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = CreateValidCheckoutRequest(999);
        var ct = CancellationToken.None;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CheckoutAsync(request, ct));
    }

    [Fact]
    public async Task CheckoutAsync_WithInsufficientQuota_ThrowsInvalidOperationException()
    {
        // Arrange
        SeedTicket(1, 100m, 1); // Only 1 quota available
        var request = CreateValidCheckoutRequest(1, 5);
        var handlerMock = MockPaymentHandler();
        _paymentFactoryMock.Setup(f => f.Resolve(request.PaymentMethod)).Returns(handlerMock.Object);
        var ct = CancellationToken.None;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CheckoutAsync(request, ct));
    }

    [Fact]
    public async Task CheckoutAsync_DecrementsTicketQuota()
    {
        // Arrange
        SeedTicket(1, 100m, 50);
        var request = CreateValidCheckoutRequest(1, 10);
        var handlerMock = MockPaymentHandler();
        _paymentFactoryMock.Setup(f => f.Resolve(request.PaymentMethod)).Returns(handlerMock.Object);
        var ct = CancellationToken.None;

        // Act
        await _service.CheckoutAsync(request, ct);

        // Assert
        var ticket = await _dbContext.Tickets.FirstAsync();
        ticket.RemainingQuota.Should().Be(40);
    }

    [Fact]
    public async Task CheckoutAsync_WithImmediatePayment_PostsLedgerEntries()
    {
        // Arrange
        SeedTicket();
        var request = CreateValidCheckoutRequest();
        var handlerMock = MockPaymentHandler(isImmediate: true);
        _paymentFactoryMock.Setup(f => f.Resolve(request.PaymentMethod)).Returns(handlerMock.Object);
        var ct = CancellationToken.None;

        // Act
        await _service.CheckoutAsync(request, ct);

        // Assert
        _ledgerServiceMock.Verify(
            l => l.PostDoubleEntryAsync(It.IsAny<Transaction>(), ct),
            Times.Once);
    }

    [Fact]
    public async Task CheckoutAsync_WithPendingPayment_EnqueuesQRConfirmation()
    {
        // Arrange
        SeedTicket();
        var request = new CheckoutRequest
        {
            TicketId = 1,
            Quantity = 2,
            CustomerName = "Jane Doe",
            CustomerEmail = "jane@example.com",
            PaymentMethod = "QR"
        };
        var handlerMock = MockPaymentHandler(isImmediate: false);
        _paymentFactoryMock.Setup(f => f.Resolve("QR")).Returns(handlerMock.Object);
        var ct = CancellationToken.None;

        // Act
        var response = await _service.CheckoutAsync(request, ct);

        // Assert
        response.Status.Should().Be("Pending");
        _ledgerServiceMock.Verify(
            l => l.PostDoubleEntryAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _qrQueueMock.Verify(q => q.Enqueue(It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task CheckoutAsync_WithImmediatePayment_SetsTransactionCompleted()
    {
        // Arrange
        SeedTicket();
        var request = CreateValidCheckoutRequest();
        var handlerMock = MockPaymentHandler(isImmediate: true);
        _paymentFactoryMock.Setup(f => f.Resolve(request.PaymentMethod)).Returns(handlerMock.Object);
        var ct = CancellationToken.None;

        // Act
        await _service.CheckoutAsync(request, ct);

        // Assert
        var transaction = await _dbContext.Transactions.FirstAsync();
        transaction.Status.Should().Be(TransactionStatus.Completed);
        transaction.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckoutAsync_WithPendingPayment_SetsTransactionPending()
    {
        // Arrange
        SeedTicket();
        var request = new CheckoutRequest
        {
            TicketId = 1,
            Quantity = 1,
            CustomerName = "Bob",
            CustomerEmail = "bob@example.com",
            PaymentMethod = "QR"
        };
        var handlerMock = MockPaymentHandler(isImmediate: false);
        _paymentFactoryMock.Setup(f => f.Resolve("QR")).Returns(handlerMock.Object);
        var ct = CancellationToken.None;

        // Act
        await _service.CheckoutAsync(request, ct);

        // Assert
        var transaction = await _dbContext.Transactions.FirstAsync();
        transaction.Status.Should().Be(TransactionStatus.Pending);
        transaction.CompletedAt.Should().BeNull();
    }

    [Fact]
    public async Task CheckoutAsync_CalculatesTotalAmountCorrectly()
    {
        // Arrange
        SeedTicket(1, 125.50m, 100);
        var request = CreateValidCheckoutRequest(1, 4);
        var handlerMock = MockPaymentHandler();
        _paymentFactoryMock.Setup(f => f.Resolve(request.PaymentMethod)).Returns(handlerMock.Object);
        var ct = CancellationToken.None;

        // Act
        var response = await _service.CheckoutAsync(request, ct);

        // Assert
        response.TotalAmount.Should().Be(502.00m);
    }

    [Fact]
    public async Task GetOrderAsync_WithValidOrderId_ReturnsOrderDto()
    {
        // Arrange
        SeedTicket();
        var request = CreateValidCheckoutRequest();
        var handlerMock = MockPaymentHandler();
        _paymentFactoryMock.Setup(f => f.Resolve(request.PaymentMethod)).Returns(handlerMock.Object);
        var checkoutResponse = await _service.CheckoutAsync(request, CancellationToken.None);
        var ct = CancellationToken.None;

        // Act
        var orderDto = await _service.GetOrderAsync(checkoutResponse.OrderId, ct);

        // Assert
        orderDto.Should().NotBeNull();
        orderDto.Id.Should().Be(checkoutResponse.OrderId);
        orderDto.Quantity.Should().Be(2);
        orderDto.TotalAmount.Should().Be(200m);
        orderDto.TicketName.Should().Be("Ticket 1");
    }

    [Fact]
    public async Task GetOrderAsync_WithInvalidOrderId_ReturnsNull()
    {
        // Arrange
        var invalidOrderId = Guid.NewGuid();
        var ct = CancellationToken.None;

        // Act
        var orderDto = await _service.GetOrderAsync(invalidOrderId, ct);

        // Assert
        orderDto.Should().BeNull();
    }

    [Fact]
    public async Task GetAllOrdersAsync_ReturnsAllOrders()
    {
        // Arrange
        SeedTicket();
        var requests = Enumerable.Range(1, 3)
            .Select(i => CreateValidCheckoutRequest())
            .ToList();
        var handlerMock = MockPaymentHandler();
        _paymentFactoryMock.Setup(f => f.Resolve(It.IsAny<string>())).Returns(handlerMock.Object);

        foreach (var request in requests)
        {
            await _service.CheckoutAsync(request, CancellationToken.None);
        }

        // Act
        var orders = await _service.GetAllOrdersAsync(CancellationToken.None);

        // Assert
        orders.Should().HaveCount(3);
        orders.Should().AllSatisfy(o => o.TicketName.Should().NotBeNullOrEmpty());
    }

    [Fact]
    public async Task GetAllOrdersAsync_ReturnsOrdersInDescendingOrder()
    {
        // Arrange
        SeedTicket();
        var handlerMock = MockPaymentHandler();
        _paymentFactoryMock.Setup(f => f.Resolve(It.IsAny<string>())).Returns(handlerMock.Object);

        var order1 = await _service.CheckoutAsync(CreateValidCheckoutRequest(), CancellationToken.None);
        await Task.Delay(10);
        var order2 = await _service.CheckoutAsync(CreateValidCheckoutRequest(), CancellationToken.None);

        // Act
        var orders = await _service.GetAllOrdersAsync(CancellationToken.None);

        // Assert
        orders[0].Id.Should().Be(order2.OrderId);
        orders[1].Id.Should().Be(order1.OrderId);
    }

    [Fact]
    public async Task GetTransactionStatusAsync_WithValidTransactionId_ReturnsStatusDto()
    {
        // Arrange
        SeedTicket();
        var request = CreateValidCheckoutRequest();
        var handlerMock = MockPaymentHandler(isImmediate: true);
        _paymentFactoryMock.Setup(f => f.Resolve(request.PaymentMethod)).Returns(handlerMock.Object);
        var checkoutResponse = await _service.CheckoutAsync(request, CancellationToken.None);
        var ct = CancellationToken.None;

        // Act
        var statusDto = await _service.GetTransactionStatusAsync(checkoutResponse.TransactionId, ct);

        // Assert
        statusDto.Should().NotBeNull();
        statusDto.TransactionId.Should().Be(checkoutResponse.TransactionId);
        statusDto.Status.Should().Be("Completed");
        statusDto.Amount.Should().Be(200m);
        statusDto.PaymentMethod.Should().Be("CreditCard");
    }

    [Fact]
    public async Task GetTransactionStatusAsync_WithInvalidTransactionId_ReturnsNull()
    {
        // Arrange
        var invalidTransactionId = Guid.NewGuid();
        var ct = CancellationToken.None;

        // Act
        var statusDto = await _service.GetTransactionStatusAsync(invalidTransactionId, ct);

        // Assert
        statusDto.Should().BeNull();
    }

    [Fact]
    public async Task CheckoutAsync_SetsOrderStatusBasedOnPayment()
    {
        // Arrange
        SeedTicket();
        var requestImmediate = CreateValidCheckoutRequest();
        var handlerMock = MockPaymentHandler(isImmediate: true);
        _paymentFactoryMock.Setup(f => f.Resolve(It.IsAny<string>())).Returns(handlerMock.Object);
        var ct = CancellationToken.None;

        // Act
        await _service.CheckoutAsync(requestImmediate, ct);

        // Assert
        var order = await _dbContext.Orders.FirstAsync();
        order.Status.Should().Be(OrderStatus.Confirmed);
    }

    [Fact]
    public async Task CheckoutAsync_WithInactiveTicket_ThrowsInvalidOperationException()
    {
        // Arrange
        var ticket = new Ticket
        {
            Id = 1,
            Name = "Inactive Ticket",
            Description = "Test",
            Price = 100m,
            TotalQuota = 50,
            RemainingQuota = 50,
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RowVersion = new byte[8],
            IsDeleted = false
        };
        _dbContext.Tickets.Add(ticket);
        _dbContext.SaveChanges();

        var request = CreateValidCheckoutRequest();
        var ct = CancellationToken.None;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CheckoutAsync(request, ct));
    }

   
}
