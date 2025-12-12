using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Models.Dto.V1.Requests;
using Oms.Config;
using Oms.Consumer.Base;
using Oms.Consumer.Clients;
using Oms.Consumer.Constants;
using OmsOrderCreatedMessage = Messages.OmsOrderCreatedMessage;
using Common;
using System.Threading;

namespace Oms.Consumer.Consumers;

public class BatchOmsOrderCreatedConsumer(
    IOptions<RabbitMqSettings> rabbitMqSettings,
    IServiceProvider serviceProvider)
    : BaseBatchMessageConsumer<OmsOrderCreatedMessage>(rabbitMqSettings.Value, s => s.OrderCreated)
{
    private static int _batchCounter;

    protected override async Task ProcessMessages(OmsOrderCreatedMessage[] messages)
    {
        var currentBatch = Interlocked.Increment(ref _batchCounter);
        if (currentBatch % 5 == 0)
        {
            throw new InvalidOperationException($"Simulated failure on batch #{currentBatch}");
        }

        using var scope = serviceProvider.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<OmsClient>();

        var orders = messages
            .SelectMany(order => order.OrderItems.Select(item =>
                new V1AuditLogOrderRequest.LogOrder
                {
                    OrderId = order.Id,
                    OrderItemId = item.Id,
                    CustomerId = order.CustomerId,
                    OrderStatus = nameof(OrderStatus.Created).ToLowerInvariant()
                }))
            .ToArray();

        if (orders.Length == 0)
        {
            return;
        }

        await client.LogOrder(
            new V1AuditLogOrderRequest
            {
                Orders = orders
            },
            CancellationToken.None);
    }
}
