namespace Oms.Config;

public class RabbitMqSettings
{
    public string HostName { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Exchange { get; set; } = string.Empty;
    public ExchangeMapping[] ExchangeMappings { get; set; } = Array.Empty<ExchangeMapping>();

    public TopicSettingsUnit OrderCreated { get; set; } = new();
    public TopicSettingsUnit OrderStatusChanged { get; set; } = new();

    public class TopicSettingsUnit
    {
        public string Queue { get; set; } = string.Empty;

        public ushort BatchSize { get; set; }

        public int BatchTimeoutSeconds { get; set; }

        public DeadLetterSettings DeadLetter { get; set; } = new();
    }

    public class ExchangeMapping
    {
        public string Queue { get; set; } = string.Empty;
        public string RoutingKeyPattern { get; set; } = string.Empty;
        public DeadLetterSettings? DeadLetter { get; set; }
    }

    public class DeadLetterSettings
    {
        public string Dlx { get; set; } = string.Empty;

        public string Dlq { get; set; } = string.Empty;

        public string RoutingKey { get; set; } = string.Empty;
    }
}
