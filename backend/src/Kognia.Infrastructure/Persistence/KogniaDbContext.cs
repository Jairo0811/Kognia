using Kognia.Domain.Catalog;
using Kognia.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Kognia.Infrastructure.Persistence;

public sealed class KogniaDbContext(DbContextOptions<KogniaDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseSection> CourseSections => Set<CourseSection>();
    public DbSet<Lesson> Lessons => Set<Lesson>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KogniaDbContext).Assembly);

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(140).IsRequired();
            entity.HasIndex(x => x.Slug).IsUnique();
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Summary).HasMaxLength(400).IsRequired();
            entity.Property(x => x.Level).HasMaxLength(50).IsRequired();
            entity.Property(x => x.InstructorUserId).HasMaxLength(450).IsRequired();
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.HasOne(x => x.Category)
                .WithMany(x => x.Courses)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CourseSection>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(180).IsRequired();
            entity.HasOne(x => x.Course)
                .WithMany(x => x.Sections)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(180).IsRequired();
            entity.Property(x => x.LessonType).HasMaxLength(30).IsRequired();
            entity.HasOne(x => x.CourseSection)
                .WithMany(x => x.Lessons)
                .HasForeignKey(x => x.CourseSectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
