using FiapGames.Notifications.Api.Domain;
using FiapGames.Shared.Kernel.Pagination;

namespace FiapGames.Notifications.Api.Application.Abstractions;

public interface INotificationRepository
{
    // Atomically claims a dedupe key by inserting the row (relying on a
    // unique index on DedupeKey). Returns true the first time a key is
    // seen — the caller should now proceed to actually send. Returns
    // false on any redelivery — the caller must not send again.
    Task<bool> TryClaimAsync(Notification notification, CancellationToken cancellationToken = default);

    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<PagedResult<Notification>> GetAllAdminAsync(PagedRequest request, string? type, string? status, DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
}
