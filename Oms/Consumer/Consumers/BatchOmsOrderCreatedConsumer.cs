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
using OmsOrderCreatedMessage = Messages.OrderCreatedMessage;

namespace Oms.Consumer.Consumers;

public class BatchOmsOrderCreatedConsumer(
    IOptions<RabbitMqSettings> rabbitMqSettings,
    IServiceProvider serviceProvider)
    : BaseBatchMessageConsumer<OmsOrderCreatedMessage>(rabbitMqSettings.Value)
{
    protected override async Task ProcessMessages(OmsOrderCreatedMessage[] messages)
    {
        using var scope = serviceProvider.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<OmsClient>();

        var orders = messages
            .SelectMany(order => order.OrderItems.Select(item =>
                new V1AuditLogOrderRequest.LogOrder
                {
                    OrderId = order.Id,
                    OrderItemId = item.Id,
                    CustomerId = order.CustomerId,
                    OrderStatus = nameof(OrderStatus.Created)
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
