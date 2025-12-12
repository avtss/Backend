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

public abstract class BaseBatchMessageConsumer<T>(
    RabbitMqSettings rabbitMqSettings,
    Func<RabbitMqSettings, RabbitMqSettings.TopicSettingsUnit> topicSelector)
    : IHostedService
    where T : Messages.BaseMessage
{
    protected readonly RabbitMqSettings Settings = rabbitMqSettings;
    private readonly RabbitMqSettings.TopicSettingsUnit _topicSettings = topicSelector(rabbitMqSettings);
    private readonly string _routingKeyPattern = ResolveRoutingKey(rabbitMqSettings, topicSelector(rabbitMqSettings));

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

    private static string ResolveRoutingKey(
        RabbitMqSettings settings,
        RabbitMqSettings.TopicSettingsUnit topic)
    {
        var mapping = settings.ExchangeMappings
            .FirstOrDefault(m => string.Equals(m.Queue, topic.Queue, StringComparison.OrdinalIgnoreCase));

        if (mapping is null)
        {
            throw new InvalidOperationException($"Routing key mapping not found for queue '{topic.Queue}'.");
        }

        return mapping.RoutingKeyPattern;
    }

    public async Task StartAsync(CancellationToken token)
    {
        _connection = await _factory.CreateConnectionAsync(token);
        _channel = await _connection.CreateChannelAsync(cancellationToken: token);

        _messageBuffer = new List<MessageInfo>();
        _processingSemaphore = new SemaphoreSlim(1, 1);

        await _channel.BasicQosAsync(0, (ushort)(_topicSettings.BatchSize * 2), false, token);

        var batchTimeout = TimeSpan.FromSeconds(_topicSettings.BatchTimeoutSeconds);
        _batchTimer = new Timer(ProcessBatchByTimeout, null, batchTimeout, batchTimeout);

        await _channel.ExchangeDeclareAsync(
            exchange: Settings.Exchange,
            type: ExchangeType.Topic,
            durable: false,
            autoDelete: false,
            cancellationToken: token);

        await _channel.ExchangeDeclareAsync(
            exchange: _topicSettings.DeadLetter.Dlx,
            type: ExchangeType.Direct,
            durable: true,
            cancellationToken: token);

        await _channel.QueueDeclareAsync(
            queue: _topicSettings.DeadLetter.Dlq,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: token);

        await _channel.QueueBindAsync(
            queue: _topicSettings.DeadLetter.Dlq,
            exchange: _topicSettings.DeadLetter.Dlx,
            routingKey: _topicSettings.DeadLetter.RoutingKey,
            cancellationToken: token);

        IDictionary<string, object?> queueArgs = new Dictionary<string, object?>
        {
            { "x-dead-letter-exchange", _topicSettings.DeadLetter.Dlx },
            { "x-dead-letter-routing-key", _topicSettings.DeadLetter.RoutingKey }
        };

        await _channel.QueueDeclareAsync(
            queue: _topicSettings.Queue,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: queueArgs,
            cancellationToken: token);

        await _channel.QueueBindAsync(
            queue: _topicSettings.Queue,
            exchange: Settings.Exchange,
            routingKey: _routingKeyPattern,
            cancellationToken: token);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnMessageReceived;

        await _channel.BasicConsumeAsync(
            queue: _topicSettings.Queue,
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

            if (_messageBuffer.Count >= _topicSettings.BatchSize)
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
            await _channel.BasicNackAsync(lastDeliveryTag, multiple: true, requeue: false);
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
