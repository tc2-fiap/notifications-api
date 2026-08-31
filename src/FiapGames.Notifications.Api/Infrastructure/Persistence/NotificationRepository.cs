using FiapGames.Notifications.Api.Application.Abstractions;
using FiapGames.Notifications.Api.Domain;
using FiapGames.Shared.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FiapGames.Notifications.Api.Infrastructure.Persistence;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly NotificationsDbContext _context;

    public NotificationRepository(NotificationsDbContext context)
    {
        _context = context;
    }

    public async Task<bool> TryClaimAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        _context.Notifications.Add(notification);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Scoped per-message by MassTransit's consumer DI scope, so the
            // context is disposed right after — no need to reset tracking.
            return false;
        }
    }

    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await _context.SaveChangesAsync(cancellationToken) >= 0;

    public async Task<IReadOnlyList<Notification>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        await _context.Notifications
            .Where(n => n.OrderId == orderId)
            .OrderBy(n => n.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<PagedResult<Notification>> GetAllAdminAsync(PagedRequest request, string? type, string? status, DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
    {
        var query = _context.Notifications.AsQueryable();

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(n => n.Type.ToString() == type);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(n => n.Status.ToString() == status);
        if (from.HasValue)
            query = query.Where(n => n.CreatedAtUtc >= from.Value);
        if (to.HasValue)
            query = query.Where(n => n.CreatedAtUtc <= to.Value);

        query = query.OrderByDescending(n => n.CreatedAtUtc);

        var totalCount = await query.LongCountAsync(cancellationToken);
        var items = await query.Skip(request.Skip).Take(request.PageSize ?? 10).ToListAsync(cancellationToken);

        return new PagedResult<Notification>(items, totalCount, request.Page ?? 1, request.PageSize ?? 10);
    }
}
