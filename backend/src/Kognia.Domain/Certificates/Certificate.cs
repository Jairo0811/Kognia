using Kognia.Domain.Catalog;

namespace Kognia.Domain.Certificates;

public sealed class Certificate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string StudentUserId { get; set; } = string.Empty;
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public string VerificationCode { get; set; } = string.Empty;
    public DateTimeOffset IssuedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
