using System;
using System.Linq;
using System.Text;
using Common;
using Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Models.Dto.V1.Requests;
using Oms.Config;
using Oms.Consumer.Clients;
using Oms.Consumer.Constants;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using OmsOrderCreatedMessage = Messages.OrderCreatedMessage;

namespace Oms.Consumer.Consumers;

public class OmsOrderCreatedConsumer : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<RabbitMqSettings> _rabbitMqSettings;
    private readonly ConnectionFactory _factory;
    private IConnection _connection;
    private IChannel _channel;
    private AsyncEventingBasicConsumer _consumer;

    public OmsOrderCreatedConsumer(IOptions<RabbitMqSettings> rabbitMqSettings, IServiceProvider serviceProvider)
    {
        _rabbitMqSettings = rabbitMqSettings;
        _serviceProvider = serviceProvider;
        _factory = new ConnectionFactory
        {
            HostName = rabbitMqSettings.Value.HostName,
            Port = rabbitMqSettings.Value.Port
        };
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _connection = await _factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: _rabbitMqSettings.Value.OrderCreatedQueue,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        _consumer = new AsyncEventingBasicConsumer(_channel);
        _consumer.ReceivedAsync += async (_, args) =>
        {
            var messageBytes = args.Body.ToArray();
            var payload = Encoding.UTF8.GetString(messageBytes);
            var order = payload.FromJson<OmsOrderCreatedMessage>();

            Console.WriteLine("Received: " + payload);

            var orderItems = order.OrderItems ?? Array.Empty<OrderItemMessage>();
            var logOrders = orderItems.Select(item => new V1AuditLogOrderRequest.LogOrder
            {
                OrderId = order.Id,
                OrderItemId = item.Id,
                CustomerId = order.CustomerId,
                OrderStatus = nameof(OrderStatus.Created)
            }).ToArray();

            if (logOrders.Length == 0)
            {
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var client = scope.ServiceProvider.GetRequiredService<OmsClient>();

            await client.LogOrder(new V1AuditLogOrderRequest
            {
                Orders = logOrders
            }, CancellationToken.None);
        };

        await _channel.BasicConsumeAsync(
            queue: _rabbitMqSettings.Value.OrderCreatedQueue,
            autoAck: true,
            consumer: _consumer,
            cancellationToken: cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _consumer = null;
        _channel?.Dispose();
        _connection?.Dispose();
        return Task.CompletedTask;
    }
}
