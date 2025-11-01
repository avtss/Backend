using System;
using System.Diagnostics;
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

        var sw = new Stopwatch();

        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: cancellationToken);
        _consumer = new AsyncEventingBasicConsumer(_channel);
        _consumer.ReceivedAsync += async (sender, args) =>
        {
            sw.Restart();
            try
            {
                var body = args.Body.ToArray();
                var payload = Encoding.UTF8.GetString(body);
                var message = payload.FromJson<OmsOrderCreatedMessage>();

                using var scope = _serviceProvider.CreateScope();
                var client = scope.ServiceProvider.GetRequiredService<OmsClient>();

                var orders = (message.OrderItems ?? Array.Empty<OrderItemMessage>())
                    .Select(item => new V1AuditLogOrderRequest.LogOrder
                    {
                        OrderId = message.Id,
                        OrderItemId = item.Id,
                        CustomerId = message.CustomerId,
                        OrderStatus = OrderStatus.Created.ToString()
                    })
                    .ToArray();

                if (orders.Length > 0)
                {
                    var request = new V1AuditLogOrderRequest
                    {
                        Orders = orders
                    };

                    await client.LogOrder(request, cancellationToken);
                }
                else
                {
                    Console.WriteLine("Received order without items; skipping audit log.");
                }
                
                await _channel.BasicAckAsync(args.DeliveryTag, false, cancellationToken);
                sw.Stop();
                Console.WriteLine($"Order created consumed in {sw.ElapsedMilliseconds} ms");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await _channel.BasicNackAsync(args.DeliveryTag, false, true, cancellationToken);
            }
        };
        
        await _channel.BasicConsumeAsync(
            queue: _rabbitMqSettings.Value.OrderCreatedQueue, 
            autoAck: false, 
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
