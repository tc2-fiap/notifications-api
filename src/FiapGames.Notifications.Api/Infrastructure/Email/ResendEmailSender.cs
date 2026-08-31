using System.Net.Http.Json;
using System.Text.Json;
using FiapGames.Notifications.Api.Domain;
using Microsoft.Extensions.Configuration;

namespace FiapGames.Notifications.Api.Infrastructure.Email;

// Real email delivery via Resend's REST API — a plain typed HttpClient
// POST, not their SDK, to keep the same "typed client" pattern already
// used for orders-api's CatalogApiClient. Optional: see notes.md (real
// email delivery entry) — instructions.md §12 previously ruled this out
// entirely; it's now config-toggled, console remains the default.
public sealed class ResendEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly string _fromAddress;

    public DeliveryChannel Channel => DeliveryChannel.Resend;

    public ResendEmailSender(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _fromAddress = configuration["Resend:FromAddress"] ?? "onboarding@resend.dev";
    }

    public async Task<EmailSendResult> SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var request = new { from = _fromAddress, to = new[] { to }, subject, html = body };
        var requestPayload = JsonSerializer.Serialize(request);

        using var response = await _httpClient.PostAsJsonAsync("emails", request, cancellationToken);
        var responsePayload = await response.Content.ReadAsStringAsync(cancellationToken);

        return new EmailSendResult(response.IsSuccessStatusCode, requestPayload, responsePayload);
    }
}
