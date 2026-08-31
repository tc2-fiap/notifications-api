using FiapGames.Notifications.Api.Domain;

namespace FiapGames.Notifications.Api.Application.Abstractions;

public interface IUserProjectionRepository
{
    Task<UserProjection?> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    Task UpsertAsync(Guid userId, string name, string email, CancellationToken cancellationToken = default);
}
