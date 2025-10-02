namespace Oms.Config;

public class RabbitMqSettings
{
    public string HostName { get; set; } = string.Empty;
    public int Port { get; set; }
    public string OrderCreatedQueue { get; set; } = string.Empty;
}
