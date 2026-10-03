using Kognia.Domain.Assessments;
using Kognia.Domain.Catalog;
using Kognia.Domain.Certificates;
using Kognia.Domain.Learning;
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
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<LessonProgress> LessonProgress => Set<LessonProgress>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<QuizAnswer> QuizAnswers => Set<QuizAnswer>();
    public DbSet<Certificate> Certificates => Set<Certificate>();

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

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.StudentUserId).HasMaxLength(450).IsRequired();
            entity.HasIndex(x => new { x.StudentUserId, x.CourseId }).IsUnique();
            entity.HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LessonProgress>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.EnrollmentId, x.LessonId }).IsUnique();
            entity.HasOne(x => x.Enrollment)
                .WithMany(x => x.LessonProgress)
                .HasForeignKey(x => x.EnrollmentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Lesson)
                .WithMany()
                .HasForeignKey(x => x.LessonId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Quiz>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(180).IsRequired();
            entity.HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Text).HasMaxLength(1000).IsRequired();
            entity.HasOne(x => x.Quiz)
                .WithMany(x => x.Questions)
                .HasForeignKey(x => x.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuestionOption>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Text).HasMaxLength(500).IsRequired();
            entity.HasOne(x => x.Question)
                .WithMany(x => x.Options)
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuizAttempt>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.StudentUserId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.ScorePercent).HasPrecision(5, 2);
            entity.HasOne(x => x.Quiz)
                .WithMany(x => x.Attempts)
                .HasForeignKey(x => x.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuizAnswer>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.QuizAttempt)
                .WithMany(x => x.Answers)
                .HasForeignKey(x => x.QuizAttemptId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Certificate>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.StudentUserId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.VerificationCode).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => x.VerificationCode).IsUnique();
            entity.HasIndex(x => new { x.StudentUserId, x.CourseId }).IsUnique();
            entity.HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
