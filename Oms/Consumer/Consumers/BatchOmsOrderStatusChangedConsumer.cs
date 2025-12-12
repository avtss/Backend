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
using StatusChangedMessage = Messages.OmsOrderStatusChangedMessage;

namespace Oms.Consumer.Consumers;

public class BatchOmsOrderStatusChangedConsumer(
    IOptions<RabbitMqSettings> rabbitMqSettings,
    IServiceProvider serviceProvider)
    : BaseBatchMessageConsumer<StatusChangedMessage>(rabbitMqSettings.Value, s => s.OrderStatusChanged)
{
    protected override async Task ProcessMessages(StatusChangedMessage[] messages)
    {
        using var scope = serviceProvider.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<OmsClient>();

        var orders = messages
            .SelectMany(msg =>
                (msg.OrderItemIds?.Length > 0 ? msg.OrderItemIds : new[] { msg.OrderId })
                .Select(orderItemId => new V1AuditLogOrderRequest.LogOrder
                {
                    OrderId = msg.OrderId,
                    OrderItemId = orderItemId,
                    CustomerId = msg.CustomerId,
                    OrderStatus = msg.OrderStatus.ToLowerInvariant()
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
