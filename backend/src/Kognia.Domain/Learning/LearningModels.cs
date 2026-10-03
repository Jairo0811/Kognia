using Kognia.Domain.Catalog;

namespace Kognia.Domain.Learning;

public enum EnrollmentStatus
{
    Active = 0,
    Completed = 1,
    Cancelled = 2
}

public sealed class Enrollment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string StudentUserId { get; set; } = string.Empty;
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
    public DateTimeOffset EnrolledAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public ICollection<LessonProgress> LessonProgress { get; set; } = new List<LessonProgress>();
}

public sealed class LessonProgress
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EnrollmentId { get; set; }
    public Enrollment Enrollment { get; set; } = null!;
    public Guid LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;
    public bool IsCompleted { get; set; }
    public int LastPositionSeconds { get; set; }
    public DateTimeOffset LastAccessedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAtUtc { get; set; }
}
