using Microsoft.Extensions.Options;
using Oms.Config;
using Messages;
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

    public async Task BatchInsert(IEnumerable<OrderCreatedMessage> messages, CancellationToken cancellationToken)
    {
        await _rabbitMqService.Publish(messages, _settings.Value.OrderCreatedQueue, cancellationToken);
    }
}