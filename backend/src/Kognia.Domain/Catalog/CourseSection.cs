namespace Kognia.Domain.Catalog;

public sealed class CourseSection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public int Position { get; set; }
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}
