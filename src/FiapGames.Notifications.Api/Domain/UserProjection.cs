namespace FiapGames.Notifications.Api.Domain;

// A local read-model of just the user fields notifications-api needs
// (an email address to send to), kept up to date from UserCreatedEvent.
// PaymentProcessedEvent carries only a UserId, not an email — and
// reshaping that fixed event contract or adding a synchronous call to
// users-api at send time would both cost more than projecting the one
// field this service actually needs from an event it already consumes.
// Cross-service DB reads stay forbidden; this is not one — it's this
// service's own schema, populated by its own consumer.
public sealed class UserProjection
{
    public Guid UserId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    private UserProjection() { }

    public UserProjection(Guid userId, string name, string email)
    {
        UserId = userId;
        Name = name;
        Email = email;
    }

    public void Update(string name, string email)
    {
        Name = name;
        Email = email;
    }
}
