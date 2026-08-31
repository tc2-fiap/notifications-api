using FiapGames.Notifications.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FiapGames.Notifications.Api.Infrastructure.Persistence;

public sealed class NotificationsDbContext : DbContext
{
    public const string Schema = "notifications";

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<UserProjection> UserProjections => Set<UserProjection>();

    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<Notification>(builder =>
        {
            builder.ToTable("notifications");
            builder.HasKey(n => n.Id);
            builder.Property(n => n.DedupeKey).IsRequired().HasMaxLength(200);
            builder.HasIndex(n => n.DedupeKey).IsUnique();
            builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(30);
            builder.Property(n => n.Channel).HasConversion<string>().HasMaxLength(20);
            builder.Property(n => n.Status).HasConversion<string>().HasMaxLength(20);
            builder.Property(n => n.Recipient).IsRequired().HasMaxLength(256);
            builder.HasIndex(n => n.OrderId);
        });

        modelBuilder.Entity<UserProjection>(builder =>
        {
            builder.ToTable("user_projections");
            builder.HasKey(u => u.UserId);
            builder.Property(u => u.Name).IsRequired().HasMaxLength(200);
            builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        });
    }
}
