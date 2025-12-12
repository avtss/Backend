using System;
using System.Linq;
using Messages;
using Oms.Services;
using WebApi.DAL;

public class OrderService(
    UnitOfWork unitOfWork,
    IOrderRepository orderRepository,
    IOrderItemRepository orderItemRepository,
    RabbitMqService rabbitMqService)
{
    /// <summary>
    /// Метод создания заказов
    /// </summary>
public async Task<OrderUnit[]> BatchInsert(OrderUnit[] orderUnits, CancellationToken token)
    {
        var now = DateTimeOffset.UtcNow;
        await using var transaction = await unitOfWork.BeginTransactionAsync(token);

        try
        {
            var ordersDal = orderUnits.Select(o => new V1OrderDal
            {
                CustomerId = o.CustomerId,
                DeliveryAddress = o.DeliveryAddress,
                TotalPriceCents = o.TotalPriceCents,
                TotalPriceCurrency = o.TotalPriceCurrency,
                OrderStatus = o.OrderStatus ?? "created",
                CreatedAt = now,
                UpdatedAt = now
            }).ToArray();

            var insertedOrders = await orderRepository.BulkInsert(ordersDal, token);

            var itemsDal = orderUnits
                .SelectMany((o, idx) => o.OrderItems.Select(i => new V1OrderItemDal
                {
                    OrderId = insertedOrders[idx].Id, 
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    ProductTitle = i.ProductTitle,
                    ProductUrl = i.ProductUrl,
                    PriceCents = i.PriceCents,
                    PriceCurrency = i.PriceCurrency,
                    CreatedAt = now,
                    UpdatedAt = now
                }))
                .ToArray();

            var insertedItems = await orderItemRepository.BulkInsert(itemsDal, token);

            await transaction.CommitAsync(token);

            var itemsLookup = insertedItems.ToLookup(x => x.OrderId);

            var result = insertedOrders.Select(o => new OrderUnit
            {
                Id = o.Id,
                CustomerId = o.CustomerId,
                DeliveryAddress = o.DeliveryAddress,
                TotalPriceCents = o.TotalPriceCents,
                TotalPriceCurrency = o.TotalPriceCurrency,
                OrderStatus = o.OrderStatus,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt,
                OrderItems = itemsLookup[o.Id].Select(i => new OrderItemUnit
                {
                    Id = i.Id,
                    OrderId = i.OrderId,
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    ProductTitle = i.ProductTitle,
                    ProductUrl = i.ProductUrl,
                    PriceCents = i.PriceCents,
                    PriceCurrency = i.PriceCurrency,
                    CreatedAt = i.CreatedAt,
                    UpdatedAt = i.UpdatedAt
                }).ToArray()
            }).ToArray();

            return result;
        }
        catch
        {
            await transaction.RollbackAsync(token);
            throw;
        }
    }
    
    /// <summary>
    /// Метод получения заказов
    /// </summary>
    public async Task<OrderUnit[]> GetOrders(QueryOrderItemsModel model, CancellationToken token)
    {
        var orders = await orderRepository.Query(new QueryOrdersDalModel
        {
            Ids = model.Ids,
            CustomerIds = model.CustomerIds,
            Limit = model.PageSize,
            Offset = (model.Page - 1) * model.PageSize
        }, token);

        if (orders.Length is 0)
        {
            return [];
        }
        
        ILookup<long, V1OrderItemDal> orderItemLookup = null;
        if (model.IncludeOrderItems)
        {
            var orderItems = await orderItemRepository.Query(new QueryOrderItemsDalModel
            {
                OrderIds = orders.Select(x => x.Id).ToArray(),
            }, token);

            orderItemLookup = orderItems.ToLookup(x => x.OrderId);
        }

        return Map(orders, orderItemLookup);
    }

    public async Task UpdateStatus(long[] orderIds, string newStatus, CancellationToken token)
    {
        if (orderIds is null || orderIds.Length == 0)
        {
            return;
        }

        var orders = await orderRepository.Query(new QueryOrdersDalModel
        {
            Ids = orderIds,
            Limit = 0,
            Offset = 0
        }, token);

        if (orders.Length == 0)
        {
            return;
        }

        var newStatusNormalized = newStatus?.Trim().ToLowerInvariant() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(newStatusNormalized))
        {
            throw new InvalidOperationException("NewStatus is required.");
        }

        var invalid = orders.Any(o =>
            string.Equals(o.OrderStatus, "created", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(newStatusNormalized, "completed", StringComparison.OrdinalIgnoreCase));

        if (invalid)
        {
            throw new InvalidOperationException("Cannot transition order from created to completed.");
        }

        var orderItems = await orderItemRepository.Query(new QueryOrderItemsDalModel
        {
            OrderIds = orderIds
        }, token);

        var itemsLookup = orderItems.ToLookup(x => x.OrderId);

        var now = DateTimeOffset.UtcNow;
        await orderRepository.UpdateStatus(orderIds, newStatusNormalized, now, token);

        var statusMessages = orders.Select(o => new OmsOrderStatusChangedMessage
        {
            OrderId = o.Id,
            OrderStatus = newStatusNormalized,
            CustomerId = o.CustomerId,
            OrderItemIds = itemsLookup[o.Id].Select(i => i.Id).ToArray(),
            UpdatedAt = now
        });

        await rabbitMqService.Publish(statusMessages, token);
    }
    
    private OrderUnit[] Map(V1OrderDal[] orders, ILookup<long, V1OrderItemDal> orderItemLookup = null)
    {
        return orders.Select(x => new OrderUnit
        {
            Id = x.Id,
            CustomerId = x.CustomerId,
            DeliveryAddress = x.DeliveryAddress,
            TotalPriceCents = x.TotalPriceCents,
            TotalPriceCurrency = x.TotalPriceCurrency,
            OrderStatus = x.OrderStatus,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            OrderItems = orderItemLookup?[x.Id].Select(o => new OrderItemUnit
            {
                Id = o.Id,
                OrderId = o.OrderId,
                ProductId = o.ProductId,
                Quantity = o.Quantity,
                ProductTitle = o.ProductTitle,
                ProductUrl = o.ProductUrl,
                PriceCents = o.PriceCents,
                PriceCurrency = o.PriceCurrency,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt
            }).ToArray() ?? []
        }).ToArray();
    }
}
