using FiapGames.Contracts;
using FiapGames.Notifications.Api.Application.Abstractions;
using FiapGames.Notifications.Api.Domain;
using FiapGames.Notifications.Api.Infrastructure.Email;
using MassTransit;

namespace FiapGames.Notifications.Api.Consumers;

public sealed class PaymentProcessedConsumer : IConsumer<PaymentProcessedEvent>
{
    private readonly INotificationRepository _repository;
    private readonly IUserProjectionRepository _userProjections;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<PaymentProcessedConsumer> _logger;

    public PaymentProcessedConsumer(
        INotificationRepository repository,
        IUserProjectionRepository userProjections,
        IEmailSender emailSender,
        ILogger<PaymentProcessedConsumer> logger)
    {
        _repository = repository;
        _userProjections = userProjections;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
    {
        var message = context.Message;

        var user = await _userProjections.GetAsync(message.UserId, context.CancellationToken);
        if (user is null)
        {
            _logger.LogError("No user projection for {UserId} — cannot address a payment notification for order {OrderId}", message.UserId, message.OrderId);
            return;
        }

        var dedupeKey = $"payment-processed:{message.OrderId}";
        var type = message.Status == PaymentStatus.Approved ? NotificationType.PurchaseConfirmation : NotificationType.PaymentFailed;
        var notification = new Notification(dedupeKey, type, message.UserId, message.OrderId, user.Email);

        if (!await _repository.TryClaimAsync(notification, context.CancellationToken))
        {
            _logger.LogInformation("Skipping duplicate payment notification for order {OrderId} — already sent", message.OrderId);
            return;
        }

        var (subject, body) = message.Status == PaymentStatus.Approved
            ? ("Your purchase is confirmed!", $"Hi {user.Name}, your order {message.OrderId} was approved. Enjoy your game!")
            : ("Payment failed", $"Hi {user.Name}, the payment for order {message.OrderId} was declined.");

        var result = await _emailSender.SendAsync(user.Email, subject, body, context.CancellationToken);

        notification.Complete(subject, body, _emailSender.Channel, result.Success ? DeliveryStatus.Sent : DeliveryStatus.Failed, result.RequestPayload, result.ResponsePayload);
        await _repository.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation("Sent {Type} notification for order {OrderId} to user {UserId}", type, message.OrderId, message.UserId);
    }
}
