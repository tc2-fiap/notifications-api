using FiapGames.Notifications.Api.Application.Abstractions;
using FiapGames.Notifications.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FiapGames.Notifications.Api.Infrastructure.Persistence;

public sealed class UserProjectionRepository : IUserProjectionRepository
{
    private readonly NotificationsDbContext _context;

    public UserProjectionRepository(NotificationsDbContext context)
    {
        _context = context;
    }

    public Task<UserProjection?> GetAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _context.UserProjections.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

    public async Task UpsertAsync(Guid userId, string name, string email, CancellationToken cancellationToken = default)
    {
        var existing = await _context.UserProjections.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (existing is null)
        {
            _context.UserProjections.Add(new UserProjection(userId, name, email));
        }
        else
        {
            existing.Update(name, email);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
