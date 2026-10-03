using Kognia.Domain.Engagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kognia.Infrastructure.Persistence;

public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> entity)
    {
        entity.HasKey(x => x.Id);
        entity.Property(x => x.StudentUserId).HasMaxLength(450).IsRequired();
        entity.Property(x => x.Comment).HasMaxLength(2000);
        entity.HasIndex(x => new { x.StudentUserId, x.CourseId }).IsUnique();
        entity.HasIndex(x => new { x.CourseId, x.IsVisible });
        entity.HasOne(x => x.Course)
            .WithMany()
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
{
    public void Configure(EntityTypeBuilder<Favorite> entity)
    {
        entity.HasKey(x => x.Id);
        entity.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        entity.HasIndex(x => new { x.UserId, x.CourseId }).IsUnique();
        entity.HasOne(x => x.Course)
            .WithMany()
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> entity)
    {
        entity.HasKey(x => x.Id);
        entity.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        entity.Property(x => x.Title).HasMaxLength(180).IsRequired();
        entity.Property(x => x.Message).HasMaxLength(1000).IsRequired();
        entity.Property(x => x.ActionUrl).HasMaxLength(500);
        entity.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAtUtc });
    }
}
