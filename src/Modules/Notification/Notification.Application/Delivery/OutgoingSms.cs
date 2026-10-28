namespace Notification.Application.Delivery;

public sealed record OutgoingSms
{
    public required string PhoneNumber { get; init; }

    public required string Body { get; init; }

    public required Guid MessageId { get; init; }
}
