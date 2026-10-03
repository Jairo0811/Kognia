namespace Kognia.Domain.Catalog;

public enum CourseStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2
}

public sealed class Course
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Level { get; set; } = "Beginner";
    public string InstructorUserId { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public CourseStatus Status { get; set; } = CourseStatus.Draft;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PublishedAtUtc { get; set; }
    public ICollection<CourseSection> Sections { get; set; } = new List<CourseSection>();
}
