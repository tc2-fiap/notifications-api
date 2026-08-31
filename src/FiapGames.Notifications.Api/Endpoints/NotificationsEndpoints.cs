using FiapGames.Notifications.Api.Application.Abstractions;
using FiapGames.Notifications.Api.Application.Dtos;
using FiapGames.Shared.Kernel.Pagination;

namespace FiapGames.Notifications.Api.Endpoints;

public static class NotificationsEndpoints
{
    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/notifications", async (Guid orderId, INotificationRepository repository, CancellationToken cancellationToken) =>
        {
            var notifications = await repository.GetByOrderIdAsync(orderId, cancellationToken);
            return Results.Ok(notifications.Select(NotificationResponse.FromDomain));
        })
        .WithTags("Notifications")
        .RequireAuthorization(p => p.RequireRole("Admin"));

        endpoints.MapGet("/api/notifications/admin", async (
            [AsParameters] PagedRequest request,
            string? type,
            string? status,
            DateTime? from,
            DateTime? to,
            INotificationRepository repository,
            CancellationToken cancellationToken) =>
        {
            var paged = await repository.GetAllAdminAsync(request, type, status, from, to, cancellationToken);
            var items = paged.Items.Select(NotificationResponse.FromDomain).ToList();
            return Results.Ok(new PagedResult<NotificationResponse>(items, paged.TotalCount, paged.Page, paged.PageSize));
        })
        .WithTags("Notifications")
        .RequireAuthorization(p => p.RequireRole("Admin"));

        return endpoints;
    }
}
