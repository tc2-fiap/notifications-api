using FiapGames.Notifications.Api.Application.Abstractions;
using FiapGames.Notifications.Api.Consumers;
using FiapGames.Notifications.Api.Endpoints;
using FiapGames.Notifications.Api.Infrastructure.Email;
using FiapGames.Notifications.Api.Infrastructure.Persistence;
using FiapGames.Shared.Infrastructure.Extensions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter()));

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddGlobalExceptionHandling();

var postgresConnectionString =
    $"Host={builder.Configuration["Postgres:Host"] ?? "localhost"};" +
    $"Port={builder.Configuration["Postgres:Port"] ?? "5432"};" +
    $"Database={builder.Configuration["Postgres:Database"] ?? "fiap_games"};" +
    $"Username={builder.Configuration["Postgres:Username"] ?? "notifications_role"};" +
    $"Password={builder.Configuration["Postgres:Password"]};" +
    $"Search Path={builder.Configuration["Postgres:SearchPath"] ?? "notifications"}";

builder.Services.AddDbContext<NotificationsDbContext>(options =>
    options.UseNpgsql(postgresConnectionString));

builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IUserProjectionRepository, UserProjectionRepository>();

// Strategy pattern selected by ConfigMap value, mirroring payments-api's
// IPaymentGateway selection exactly. Console is the default and the
// reliable demo signal; Resend is opt-in. See instructions.md §12 and
// notes.md (real email delivery entry).
var emailProvider = builder.Configuration["Email:Provider"] ?? "console";
switch (emailProvider)
{
    case "resend":
        builder.Services.AddHttpClient<IEmailSender, ResendEmailSender>(client =>
        {
            client.BaseAddress = new Uri("https://api.resend.com/");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", builder.Configuration["Resend:ApiKey"]);
        });
        break;

    case "console":
    default:
        builder.Services.AddScoped<IEmailSender, ConsoleEmailSender>();
        break;
}

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<UserCreatedConsumer>();
    x.AddConsumer<PaymentProcessedConsumer>();
    x.AddConsumer<TokenRevokedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(
            builder.Configuration["RabbitMq:Host"] ?? "localhost",
            builder.Configuration["RabbitMq:VirtualHost"] ?? "/",
            h =>
            {
                h.Username(builder.Configuration["RabbitMq:Username"] ?? "guest");
                h.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
            });

        // Explicit, service-scoped endpoint names — see orders-api's
        // Program.cs for why relying on MassTransit's default naming
        // (which ignores the namespace) is unsafe once two services
        // declare a same-named consumer class for the same event.
        cfg.ReceiveEndpoint("notifications-api-user-created", e =>
        {
            e.ConfigureConsumer<UserCreatedConsumer>(context);
        });

        cfg.ReceiveEndpoint("notifications-api-payment-processed", e =>
        {
            e.ConfigureConsumer<PaymentProcessedConsumer>(context);
        });

        cfg.ReceiveEndpoint("notifications-api-token-revoked", e =>
        {
            e.ConfigureConsumer<TokenRevokedConsumer>(context);
        });
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
    db.Database.Migrate();
}

app.UseExceptionHandler();

app.UseSerilogRequestLogging();

app.UseAuthentication();
app.UseAuthorization();

// Shared by two routes below: the bare /version (unauthenticated, reached
// only via kubectl port-forward — not under /api/* so the Ingress can't
// route to it) and /api/notifications/version (Admin-gated, reached
// through the Ingress like any other route — the admin dashboard's source
// for this). Same handler, not duplicated logic, registered at two paths
// because nothing else makes the unauthenticated one reachable from a browser.
static IResult GetVersion()
{
    var (sha, buildTime) = FiapGames.Shared.Infrastructure.BuildInfo.Read();
    return Results.Ok(new { sha, buildTime });
}

app.MapHealthChecks("/health");
app.MapGet("/version", GetVersion);

app.MapNotificationsEndpoints(GetVersion);

try
{
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
