using System;
using System.Linq;
using AutoFixture;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Oms.Services;
using Models.Dto.Common;
using Messages;

namespace Oms.Jobs;

public class OrderGenerator(IServiceProvider serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var fixture = new Fixture();
        using var scope = serviceProvider.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<OrderService>();
        var rabbitMqService = scope.ServiceProvider.GetRequiredService<RabbitMqService>();
        var random = new Random();

        while (!stoppingToken.IsCancellationRequested)
        {
            var orders = Enumerable.Range(1, 50)
                .Select(_ =>
                {
                    var orderItem = fixture.Build<OrderItemUnit>()
                        .With(x => x.PriceCurrency, "RUB")
                        .With(x => x.PriceCents, 1000)
                        .Create();

                    var order = fixture.Build<OrderUnit>()
                        .With(x => x.TotalPriceCurrency, "RUB")
                        .With(x => x.TotalPriceCents, 1000)
                        .With(x => x.OrderStatus, "created")
                        .With(x => x.OrderItems, new[] { orderItem })
                        .Create();

                    return order;
                })
                .ToArray();

            await orderService.BatchInsert(orders, stoppingToken);

            var countToUpdate = random.Next(1, orders.Length + 1);
            var statuses = new[] { "processing", "shipped", "cancelled" };

            var statusMessages = orders
                .OrderBy(_ => random.Next())
                .Take(countToUpdate)
                .Select(o =>
                {
                    var status = statuses[random.Next(statuses.Length)];
                    return new OmsOrderStatusChangedMessage
                    {
                        OrderId = o.Id,
                        CustomerId = o.CustomerId,
                        OrderStatus = status,
                        OrderItemIds = o.OrderItems?.Select(i => i.Id).ToArray() ?? Array.Empty<long>(),
                        UpdatedAt = DateTimeOffset.UtcNow
                    };
                })
                .ToArray();

            if (statusMessages.Length > 0)
            {
                await rabbitMqService.Publish(statusMessages, stoppingToken);
            }

            await Task.Delay(250, stoppingToken);
        }
    }
}
