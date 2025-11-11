using System;

namespace Oms.Consumer.Base;

public class MessageInfo
{
    public string Message { get; set; } = string.Empty;
    public ulong DeliveryTag { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
}
