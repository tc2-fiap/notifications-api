using System.Text.Json;
using FiapGames.Notifications.Api.Domain;

namespace FiapGames.Notifications.Api.Infrastructure.Email;

// Default. Simulates outbound email by logging to the console — no real
// mail provider. See instructions.md §4.5.
public sealed class ConsoleEmailSender : IEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _logger;

    public DeliveryChannel Channel => DeliveryChannel.Console;

    public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger)
    {
        _logger = logger;
    }

    public Task<EmailSendResult> SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Simulated email to {Recipient}: {Subject}", to, subject);

        var payload = JsonSerializer.Serialize(new { channel = "console", to, subject, body });
        return Task.FromResult(new EmailSendResult(true, payload, payload));
    }
}
