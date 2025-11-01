using System;
using System.Linq;
using Messages;
using Microsoft.Extensions.Options;
using Models.Dto.Common;
using Oms.Config;

namespace Oms.Services;

public class OrderService
{
    private readonly RabbitMqService _rabbitMqService;
    private readonly IOptions<RabbitMqSettings> _settings;

    public OrderService(RabbitMqService rabbitMqService, IOptions<RabbitMqSettings> settings)
    {
        _rabbitMqService = rabbitMqService;
        _settings = settings;
    }

    public Task BatchInsert(IEnumerable<OrderUnit> orders, CancellationToken cancellationToken)
    {
        var messages = orders.Select(order => new OrderCreatedMessage
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            DeliveryAddress = order.DeliveryAddress,
            TotalPriceCents = order.TotalPriceCents,
            TotalPriceCurrency = order.TotalPriceCurrency,
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

    public async Task BatchInsert(IEnumerable<OrderCreatedMessage> messages, CancellationToken cancellationToken)
    {
        await _rabbitMqService.Publish(messages, _settings.Value.OrderCreatedQueue, cancellationToken);
    }
}
