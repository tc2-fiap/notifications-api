using FiapGames.Shared.Kernel.Entities;

namespace FiapGames.Notifications.Api.Domain;

public enum NotificationType
{
    Welcome,
    PurchaseConfirmation,
    PaymentFailed
}

public enum DeliveryChannel
{
    Console,
    Resend
}

public enum DeliveryStatus
{
    Pending,
    Sent,
    Failed
}

// A durable, idempotent record of one notification. The row is claimed
// (inserted, relying on a unique index on DedupeKey) *before* any send is
// attempted, so a redelivered event can never trigger a second real send —
// not just a second log line. See instructions.md §10 and CLAUDE.md's
// "every consumer is idempotent" rule.
public sealed class Notification : Entity
{
    public string DedupeKey { get; private set; } = string.Empty;

    public NotificationType Type { get; private set; }

    public Guid UserId { get; private set; }

    // Null for Welcome — registration isn't tied to an order.
    public Guid? OrderId { get; private set; }

    public string Recipient { get; private set; } = string.Empty;

    public string Subject { get; private set; } = string.Empty;

    public string Body { get; private set; } = string.Empty;

    public DeliveryChannel Channel { get; private set; }

    public DeliveryStatus Status { get; private set; }

    // The actual request/response exchanged with the email provider (null
    // while Console — there is no provider call to capture).
    public string? ProviderRequestPayload { get; private set; }

    public string? ProviderResponsePayload { get; private set; }

    private Notification() { }

    public Notification(string dedupeKey, NotificationType type, Guid userId, Guid? orderId, string recipient)
    {
        DedupeKey = dedupeKey;
        Type = type;
        UserId = userId;
        OrderId = orderId;
        Recipient = recipient;
        Status = DeliveryStatus.Pending;
    }

    public void Complete(
        string subject,
        string body,
        DeliveryChannel channel,
        DeliveryStatus status,
        string? providerRequestPayload,
        string? providerResponsePayload)
    {
        Subject = subject;
        Body = body;
        Channel = channel;
        Status = status;
        ProviderRequestPayload = providerRequestPayload;
        ProviderResponsePayload = providerResponsePayload;
        Touch();
    }
}
