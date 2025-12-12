using System;
using System.Linq;
using Messages;
using Models.Dto.Common;

namespace Oms.Services;

public class OrderService
{
    private readonly RabbitMqService _rabbitMqService;

    public OrderService(RabbitMqService rabbitMqService)
    {
        _rabbitMqService = rabbitMqService;
    }

    public Task BatchInsert(IEnumerable<OrderUnit> orders, CancellationToken cancellationToken)
    {
        var messages = orders.Select(order => new OmsOrderCreatedMessage
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            DeliveryAddress = order.DeliveryAddress,
            TotalPriceCents = order.TotalPriceCents,
            TotalPriceCurrency = order.TotalPriceCurrency,
            OrderStatus = order.OrderStatus ?? "created",
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            OrderItems = (order.OrderItems ?? Array.Empty<OrderItemUnit>())
                .Select(item => new OrderItemMessage
                {
                    Id = item.Id,
                    OrderId = item.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    ProductTitle = item.ProductTitle,
                    ProductUrl = item.ProductUrl,
                    PriceCents = item.PriceCents,
                    PriceCurrency = item.PriceCurrency,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt
                })
                .ToArray()
        });

        return BatchInsert(messages, cancellationToken);
    }

    public async Task BatchInsert(IEnumerable<OmsOrderCreatedMessage> messages, CancellationToken cancellationToken)
    {
        await _rabbitMqService.Publish(messages, cancellationToken);
    }
}
