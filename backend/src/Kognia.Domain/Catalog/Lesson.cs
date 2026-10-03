namespace Kognia.Domain.Catalog;

public sealed class Lesson
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CourseSectionId { get; set; }
    public CourseSection CourseSection { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string LessonType { get; set; } = "Text";
    public string? Content { get; set; }
    public string? VideoUrl { get; set; }
    public int Position { get; set; }
    public bool IsPreview { get; set; }
}
