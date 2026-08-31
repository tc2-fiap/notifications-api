using FiapGames.Notifications.Api.Domain;

namespace FiapGames.Notifications.Api.Infrastructure.Email;

public sealed record EmailSendResult(bool Success, string? RequestPayload, string? ResponsePayload);

// Strategy pattern selected by config (EMAIL_PROVIDER), exactly mirroring
// payments-api's IPaymentGateway — console is the default/reliable demo
// signal, Resend is opt-in. See notes.md (real email delivery entry).
public interface IEmailSender
{
    DeliveryChannel Channel { get; }

    Task<EmailSendResult> SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}
