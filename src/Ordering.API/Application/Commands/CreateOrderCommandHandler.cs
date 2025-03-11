using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using eShop.Ordering.API.Application.IntegrationEvents;
using eShop.Ordering.API.Application.Telemetry;
using Microsoft.Extensions.Logging;

namespace eShop.Ordering.API.Application.Commands;

// Regular CommandHandler
public class CreateOrderCommandHandler
    : IRequestHandler<CreateOrderCommand, bool>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IIdentityService _identityService;
    private readonly IMediator _mediator;
    private readonly IOrderingIntegrationEventService _orderingIntegrationEventService;
    private readonly ILogger<CreateOrderCommandHandler> _logger;
    private readonly OrderingTelemetry _telemetry;

    // Using DI to inject infrastructure persistence Repositories
    public CreateOrderCommandHandler(IMediator mediator,
        IOrderingIntegrationEventService orderingIntegrationEventService,
        IOrderRepository orderRepository,
        IIdentityService identityService,
        ILogger<CreateOrderCommandHandler> logger,
        OrderingTelemetry telemetry)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _identityService = identityService ?? throw new ArgumentNullException(nameof(identityService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _orderingIntegrationEventService = orderingIntegrationEventService ?? throw new ArgumentNullException(nameof(orderingIntegrationEventService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
    }

    public async Task<bool> Handle(CreateOrderCommand message, CancellationToken cancellationToken)
    {
        using var activity = OrderingTelemetry.ActivitySource.StartActivity("CreateOrder");

        activity?.SetTag("userId", message.UserId ?? "unknown");
        activity?.SetTag("orderItems", message.OrderItems?.Count() ?? 0);

        try
        {
            using var integrationActivity = OrderingTelemetry.ActivitySource.StartActivity("OrderStarted_IntegrationEvent");
            // Add Integration event to clean the basket
            var orderStartedIntegrationEvent = new OrderStartedIntegrationEvent(message.UserId);
            await _orderingIntegrationEventService.AddAndSaveEventAsync(orderStartedIntegrationEvent);

            // Add/Update the Buyer AggregateRoot
            // DDD patterns comment: Add child entities and value-objects through the Order Aggregate-Root
            // methods and constructor so validations, invariants and business logic 
            // make sure that consistency is preserved across the whole aggregate
            var address = new Address(message.Street, message.City, message.State, message.Country, message.ZipCode);

            using var orderCreationActivity = OrderingTelemetry.ActivitySource.StartActivity("CreateOrder_DomainLogic");

            var order = new eShop.Ordering.Domain.AggregatesModel.OrderAggregate.Order(message.UserId, message.UserName, address, message.CardTypeId, message.CardNumber, message.CardSecurityNumber, message.CardHolderName, message.CardExpiration);

            foreach (var item in message.OrderItems)
            {
                order.AddOrderItem(item.ProductId, item.ProductName, item.UnitPrice, item.Discount, item.PictureUrl, item.Units);
            }

            _logger.LogInformation("Creating Order - Order: {@Order}", order);

            // Database operations
            using var dbActivity = OrderingTelemetry.ActivitySource.StartActivity("CreateOrder_Repository");
            _orderRepository.Add(order);

            var result = await _orderRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            // Record success metric
            if (result)
            {
                _telemetry.OrdersCreatedCounter.Add(1);
                activity?.SetTag("orderCreated", true);
            }

            return result;
        }
        catch (Exception ex)
        {
            // Record failure metric
            _telemetry.OrdersFailedCounter.Add(1);

            activity?.SetTag("error", true);
            activity?.SetTag("exception", ex.ToString());

            _logger.LogError(ex, "Error creating order for user {UserId}", message.UserId);
            throw;
        }
    }
}


// Use for Idempotency in Command process
public class CreateOrderIdentifiedCommandHandler : IdentifiedCommandHandler<CreateOrderCommand, bool>
{
    public CreateOrderIdentifiedCommandHandler(
        IMediator mediator,
        IRequestManager requestManager,
        ILogger<IdentifiedCommandHandler<CreateOrderCommand, bool>> logger)
        : base(mediator, requestManager, logger)
    {
    }

    protected override bool CreateResultForDuplicateRequest()
    {
        return true; // Ignore duplicate requests for creating order.
    }
}
