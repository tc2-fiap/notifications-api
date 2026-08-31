using FiapGames.Contracts;
using FiapGames.Notifications.Api.Application.Abstractions;
using FiapGames.Notifications.Api.Domain;
using FiapGames.Notifications.Api.Infrastructure.Email;
using MassTransit;

namespace FiapGames.Notifications.Api.Consumers;

public sealed class UserCreatedConsumer : IConsumer<UserCreatedEvent>
{
    private readonly INotificationRepository _repository;
    private readonly IUserProjectionRepository _userProjections;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<UserCreatedConsumer> _logger;

    public UserCreatedConsumer(
        INotificationRepository repository,
        IUserProjectionRepository userProjections,
        IEmailSender emailSender,
        ILogger<UserCreatedConsumer> logger)
    {
        _repository = repository;
        _userProjections = userProjections;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserCreatedEvent> context)
    {
        var message = context.Message;

        // Kept up to date regardless of dedupe outcome — a redelivered
        // UserCreatedEvent (e.g. after a profile update republish) should
        // still refresh the projection PaymentProcessedEvent later reads
        // an email address from.
        await _userProjections.UpsertAsync(message.UserId, message.Name, message.Email, context.CancellationToken);

        var dedupeKey = $"user-created:{message.UserId}";
        var notification = new Notification(dedupeKey, NotificationType.Welcome, message.UserId, orderId: null, message.Email);

        if (!await _repository.TryClaimAsync(notification, context.CancellationToken))
        {
            _logger.LogInformation("Skipping duplicate welcome email for user {UserId} — already sent", message.UserId);
            return;
        }

        var subject = "Welcome to FIAP Games!";
        var body = $"Hi {message.Name}, your account is ready.";

        var result = await _emailSender.SendAsync(message.Email, subject, body, context.CancellationToken);

        notification.Complete(subject, body, _emailSender.Channel, result.Success ? DeliveryStatus.Sent : DeliveryStatus.Failed, result.RequestPayload, result.ResponsePayload);
        await _repository.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation("Sent welcome email to {Email} for user {UserId} ({Name})", message.Email, message.UserId, message.Name);
    }
}
