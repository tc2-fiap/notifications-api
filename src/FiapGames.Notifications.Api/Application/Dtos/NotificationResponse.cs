namespace FiapGames.Notifications.Api.Application.Dtos;

public sealed record NotificationResponse(
    Guid Id,
    string Type,
    Guid UserId,
    Guid? OrderId,
    string Recipient,
    string Subject,
    string Body,
    string Channel,
    string Status,
    string? ProviderRequestPayload,
    string? ProviderResponsePayload,
    DateTime CreatedAtUtc)
{
    public static NotificationResponse FromDomain(Domain.Notification notification) => new(
        notification.Id,
        notification.Type.ToString(),
        notification.UserId,
        notification.OrderId,
        notification.Recipient,
        notification.Subject,
        notification.Body,
        notification.Channel.ToString(),
        notification.Status.ToString(),
        notification.ProviderRequestPayload,
        notification.ProviderResponsePayload,
        notification.CreatedAtUtc);
}
