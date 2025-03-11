using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace eShop.Ordering.API.Application.Telemetry;

public class OrderingTelemetry
{

    public static readonly string ServiceName = "ordering-api";
    public static readonly ActivitySource ActivitySource = new(ServiceName);
    public static readonly Meter Meter = new(ServiceName);

    // Metrics
    public readonly Counter<int> OrdersCreatedCounter;
    public readonly Counter<int> OrdersFailedCounter;

    public OrderingTelemetry()
    {
        OrdersCreatedCounter = Meter.CreateCounter<int>("orders.created", "Number of orders created");
        OrdersFailedCounter = Meter.CreateCounter<int>("orders.failed", "Number of failed orders");
    }
}
