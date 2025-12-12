namespace Common;

public static class RabbitMqRoutingKeys
{
    public const string OmsOrderCreatedRoutingKey = "order.created";
    public const string OmsOrderStatusChangedRoutingKey = "order.status.changed";
}
