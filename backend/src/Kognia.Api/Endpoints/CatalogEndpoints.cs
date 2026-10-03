using System.Security.Claims;
using Kognia.Domain.Catalog;
using Kognia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kognia.Api.Endpoints;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var catalog = app.MapGroup("/api/catalog").WithTags("Catalog");
        catalog.MapGet("/categories", GetCategoriesAsync);
        catalog.MapGet("/courses", GetCoursesAsync);
        catalog.MapGet("/courses/{slug}", GetCourseAsync);

        var instructor = app.MapGroup("/api/instructor").WithTags("Instructor")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Administrator"));

        instructor.MapGet("/courses", GetInstructorCoursesAsync);
        instructor.MapPost("/courses", CreateCourseAsync);
        instructor.MapPut("/courses/{id:guid}", UpdateCourseAsync);
        instructor.MapPost("/courses/{id:guid}/publish", PublishCourseAsync);
        instructor.MapPost("/courses/{courseId:guid}/sections", AddSectionAsync);
        instructor.MapPost("/sections/{sectionId:guid}/lessons", AddLessonAsync);

        return app;
    }

    private static async Task<IResult> GetCategoriesAsync(KogniaDbContext db) =>
        Results.Ok(await db.Categories.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.Slug, x.Description }).ToListAsync());

    private static async Task<IResult> GetCoursesAsync(KogniaDbContext db, string? q, Guid? categoryId, string? level)
    {
        var query = db.Courses.AsNoTracking().Where(x => x.Status == CourseStatus.Published);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.Title.Contains(q) || x.Summary.Contains(q));
        if (categoryId.HasValue)
            query = query.Where(x => x.CategoryId == categoryId.Value);
        if (!string.IsNullOrWhiteSpace(level))
            query = query.Where(x => x.Level == level);

        var courses = await query.OrderByDescending(x => x.PublishedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Slug,
                x.Summary,
                x.Level,
                Category = x.Category.Name,
                x.InstructorUserId,
                Sections = x.Sections.Count,
                Lessons = x.Sections.SelectMany(s => s.Lessons).Count()
            }).ToListAsync();

        return Results.Ok(courses);
    }

    private static async Task<IResult> GetCourseAsync(string slug, KogniaDbContext db)
    {
        var course = await db.Courses.AsNoTracking()
            .Where(x => x.Slug == slug && x.Status == CourseStatus.Published)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Slug,
                x.Summary,
                x.Description,
                x.Level,
                Category = x.Category.Name,
                x.InstructorUserId,
                Sections = x.Sections.OrderBy(s => s.Position).Select(s => new
                {
                    s.Id,
                    s.Title,
                    s.Position,
                    Lessons = s.Lessons.OrderBy(l => l.Position).Select(l => new
                    {
                        l.Id,
                        l.Title,
                        l.LessonType,
                        l.Position,
                        l.IsPreview
                    })
                })
            }).SingleOrDefaultAsync();

        return course is null ? Results.NotFound() : Results.Ok(course);
    }

    private static async Task<IResult> GetInstructorCoursesAsync(ClaimsPrincipal principal, KogniaDbContext db)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Results.Unauthorized();

        var isAdmin = principal.IsInRole("Administrator");
        var query = db.Courses.AsNoTracking().AsQueryable();
        if (!isAdmin) query = query.Where(x => x.InstructorUserId == userId);

        return Results.Ok(await query.OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new { x.Id, x.Title, x.Slug, x.Status, x.Level, x.CategoryId, x.CreatedAtUtc, x.PublishedAtUtc })
            .ToListAsync());
    }

    private static async Task<IResult> CreateCourseAsync(CreateCourseRequest request, ClaimsPrincipal principal, KogniaDbContext db)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Results.Unauthorized();
        if (!await db.Categories.AnyAsync(x => x.Id == request.CategoryId))
            return Results.BadRequest(new { message = "Category does not exist." });
        if (await db.Courses.AnyAsync(x => x.Slug == request.Slug))
            return Results.Conflict(new { message = "Course slug already exists." });

        var course = new Course
        {
            Title = request.Title.Trim(),
            Slug = request.Slug.Trim().ToLowerInvariant(),
            Summary = request.Summary.Trim(),
            Description = request.Description.Trim(),
            Level = request.Level.Trim(),
            CategoryId = request.CategoryId,
            InstructorUserId = userId
        };

        db.Courses.Add(course);
        await db.SaveChangesAsync();
        return Results.Created($"/api/instructor/courses/{course.Id}", new { course.Id, course.Title, course.Slug, course.Status });
    }

    private static async Task<IResult> UpdateCourseAsync(Guid id, UpdateCourseRequest request, ClaimsPrincipal principal, KogniaDbContext db)
    {
        var course = await db.Courses.FindAsync(id);
        if (course is null) return Results.NotFound();
        if (!CanManage(principal, course)) return Results.Forbid();
        if (!await db.Categories.AnyAsync(x => x.Id == request.CategoryId))
            return Results.BadRequest(new { message = "Category does not exist." });

        course.Title = request.Title.Trim();
        course.Summary = request.Summary.Trim();
        course.Description = request.Description.Trim();
        course.Level = request.Level.Trim();
        course.CategoryId = request.CategoryId;
        await db.SaveChangesAsync();
        return Results.Ok(new { course.Id, course.Title, course.Status });
    }

    private static async Task<IResult> PublishCourseAsync(Guid id, ClaimsPrincipal principal, KogniaDbContext db)
    {
        var course = await db.Courses.Include(x => x.Sections).ThenInclude(x => x.Lessons).SingleOrDefaultAsync(x => x.Id == id);
        if (course is null) return Results.NotFound();
        if (!CanManage(principal, course)) return Results.Forbid();
        if (course.Sections.Count == 0 || course.Sections.SelectMany(x => x.Lessons).Count() == 0)
            return Results.BadRequest(new { message = "A course needs at least one section and one lesson before publishing." });

        course.Status = CourseStatus.Published;
        course.PublishedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return Results.Ok(new { course.Id, course.Status, course.PublishedAtUtc });
    }

    private static async Task<IResult> AddSectionAsync(Guid courseId, CreateSectionRequest request, ClaimsPrincipal principal, KogniaDbContext db)
    {
        var course = await db.Courses.FindAsync(courseId);
        if (course is null) return Results.NotFound();
        if (!CanManage(principal, course)) return Results.Forbid();

        var section = new CourseSection { CourseId = courseId, Title = request.Title.Trim(), Position = request.Position };
        db.CourseSections.Add(section);
        await db.SaveChangesAsync();
        return Results.Created($"/api/instructor/sections/{section.Id}", section);
    }

    private static async Task<IResult> AddLessonAsync(Guid sectionId, CreateLessonRequest request, ClaimsPrincipal principal, KogniaDbContext db)
    {
        var section = await db.CourseSections.Include(x => x.Course).SingleOrDefaultAsync(x => x.Id == sectionId);
        if (section is null) return Results.NotFound();
        if (!CanManage(principal, section.Course)) return Results.Forbid();

        var lesson = new Lesson
        {
            CourseSectionId = sectionId,
            Title = request.Title.Trim(),
            LessonType = request.LessonType.Trim(),
            Content = request.Content,
            VideoUrl = request.VideoUrl,
            Position = request.Position,
            IsPreview = request.IsPreview
        };
        db.Lessons.Add(lesson);
        await db.SaveChangesAsync();
        return Results.Created($"/api/instructor/lessons/{lesson.Id}", new { lesson.Id, lesson.Title, lesson.Position });
    }

    private static bool CanManage(ClaimsPrincipal principal, Course course) =>
        principal.IsInRole("Administrator") || course.InstructorUserId == principal.FindFirstValue(ClaimTypes.NameIdentifier);

    public sealed record CreateCourseRequest(string Title, string Slug, string Summary, string Description, string Level, Guid CategoryId);
    public sealed record UpdateCourseRequest(string Title, string Summary, string Description, string Level, Guid CategoryId);
    public sealed record CreateSectionRequest(string Title, int Position);
    public sealed record CreateLessonRequest(string Title, string LessonType, string? Content, string? VideoUrl, int Position, bool IsPreview);
}
