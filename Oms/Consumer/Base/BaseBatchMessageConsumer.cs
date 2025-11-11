using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Microsoft.Extensions.Hosting;
using Oms.Config;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Oms.Consumer.Base;

public abstract class BaseBatchMessageConsumer<T>(RabbitMqSettings rabbitMqSettings) : IHostedService
    where T : class
{
    private readonly RabbitMqSettings _settings = rabbitMqSettings;
    private readonly ConnectionFactory _factory = new()
    {
        HostName = rabbitMqSettings.HostName,
        Port = rabbitMqSettings.Port
    };

    private IConnection? _connection;
    private IChannel? _channel;
    private List<MessageInfo>? _messageBuffer;
    private Timer? _batchTimer;
    private SemaphoreSlim? _processingSemaphore;

    protected abstract Task ProcessMessages(T[] messages);

    public async Task StartAsync(CancellationToken token)
    {
        _connection = await _factory.CreateConnectionAsync(token);
        _channel = await _connection.CreateChannelAsync(cancellationToken: token);

        _messageBuffer = new List<MessageInfo>();
        _processingSemaphore = new SemaphoreSlim(1, 1);

        await _channel.BasicQosAsync(0, (ushort)(_settings.BatchSize * 2), false, token);

        var batchTimeout = TimeSpan.FromSeconds(_settings.BatchTimeoutSeconds);
        _batchTimer = new Timer(ProcessBatchByTimeout, null, batchTimeout, batchTimeout);

        await _channel.QueueDeclareAsync(
            queue: _settings.OrderCreatedQueue,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: token);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnMessageReceived;

        await _channel.BasicConsumeAsync(
            queue: _settings.OrderCreatedQueue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: token);
    }

    private async Task OnMessageReceived(object sender, BasicDeliverEventArgs ea)
    {
        if (_processingSemaphore is null || _messageBuffer is null)
        {
            return;
        }

        await _processingSemaphore.WaitAsync();

        try
        {
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
            _messageBuffer.Add(new MessageInfo
            {
                Message = message,
                DeliveryTag = ea.DeliveryTag,
                ReceivedAt = DateTimeOffset.UtcNow
            });

            if (_messageBuffer.Count >= _settings.BatchSize)
            {
                await ProcessBatch();
            }
        }
        finally
        {
            _processingSemaphore.Release();
        }
    }

    private async void ProcessBatchByTimeout(object? state)
    {
        if (_processingSemaphore is null)
        {
            return;
        }

        await _processingSemaphore.WaitAsync();

        try
        {
            if (_messageBuffer is { Count: > 0 })
            {
                await ProcessBatch();
            }
        }
        finally
        {
            _processingSemaphore.Release();
        }
    }

    private async Task ProcessBatch()
    {
        if (_channel is null || _messageBuffer is null || _messageBuffer.Count == 0)
        {
            return;
        }

        var currentBatch = _messageBuffer.ToList();
        _messageBuffer.Clear();

        var lastDeliveryTag = currentBatch.Max(x => x.DeliveryTag);

        try
        {
            var messages = currentBatch
                .Select(x => x.Message.FromJson<T>())
                .ToArray();

            await ProcessMessages(messages);

            await _channel.BasicAckAsync(lastDeliveryTag, multiple: true);
            Console.WriteLine($"Successfully processed batch of {currentBatch.Count} messages");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to process batch: {ex.Message}");
            await _channel.BasicNackAsync(lastDeliveryTag, multiple: true, requeue: true);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _batchTimer?.Dispose();
        _channel?.Dispose();
        _connection?.Dispose();
        _processingSemaphore?.Dispose();
        return Task.CompletedTask;
    }
}
