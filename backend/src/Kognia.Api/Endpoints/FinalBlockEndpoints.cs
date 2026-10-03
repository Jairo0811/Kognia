using System.Security.Claims;
using Kognia.Domain.Billing;
using Kognia.Domain.Catalog;
using Kognia.Domain.Engagement;
using Kognia.Infrastructure.Identity;
using Kognia.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Kognia.Api.Endpoints;

public static class FinalBlockEndpoints
{
    public static IEndpointRouteBuilder MapFinalBlockEndpoints(this IEndpointRouteBuilder app)
    {
        var analytics = app.MapGroup("/api/analytics").WithTags("Analytics").RequireAuthorization();
        analytics.MapGet("/student", GetStudentAsync);
        analytics.MapGet("/instructor", GetInstructorAsync).RequireAuthorization(p => p.RequireRole("Instructor", "Administrator"));
        analytics.MapGet("/admin", GetAdminAsync).RequireAuthorization(p => p.RequireRole("Administrator"));
        return app;
    }

    private static string? UserId(ClaimsPrincipal principal) => principal.FindFirstValue(ClaimTypes.NameIdentifier);

    private static async Task<IResult> GetStudentAsync(ClaimsPrincipal principal, KogniaDbContext db)
    {
        var userId = UserId(principal);
        if (userId is null) return Results.Unauthorized();
        var enrollments = await db.Enrollments.AsNoTracking().Where(x => x.StudentUserId == userId)
            .Select(x => new { x.CompletedAtUtc, Total = x.Course.Sections.SelectMany(s => s.Lessons).Count(), Done = x.LessonProgress.Count(p => p.IsCompleted) })
            .ToListAsync();
        var total = enrollments.Sum(x => x.Total);
        var done = enrollments.Sum(x => x.Done);
        var attempts = await db.QuizAttempts.CountAsync(x => x.StudentUserId == userId);
        var passed = await db.QuizAttempts.CountAsync(x => x.StudentUserId == userId && x.Passed);
        return Results.Ok(new {
            coursesStarted = enrollments.Count,
            coursesCompleted = enrollments.Count(x => x.CompletedAtUtc != null),
            totalLessons = total,
            completedLessons = done,
            completionPercent = total == 0 ? 0m : Math.Round(done * 100m / total, 2),
            quizAttempts = attempts,
            passedQuizAttempts = passed,
            quizPassRatePercent = attempts == 0 ? 0m : Math.Round(passed * 100m / attempts, 2),
            certificates = await db.Certificates.CountAsync(x => x.StudentUserId == userId),
            favorites = await db.Set<Favorite>().CountAsync(x => x.UserId == userId)
        });
    }

    private static async Task<IResult> GetInstructorAsync(ClaimsPrincipal principal, KogniaDbContext db)
    {
        var userId = UserId(principal);
        if (userId is null) return Results.Unauthorized();
        var courseQuery = db.Courses.AsNoTracking();
        if (!principal.IsInRole("Administrator")) courseQuery = courseQuery.Where(x => x.InstructorUserId == userId);
        var ids = await courseQuery.Select(x => x.Id).ToListAsync();
        var enrollments = await db.Enrollments.CountAsync(x => ids.Contains(x.CourseId));
        var completions = await db.Enrollments.CountAsync(x => ids.Contains(x.CourseId) && x.CompletedAtUtc != null);
        var reviews = await db.Set<Review>().Where(x => ids.Contains(x.CourseId) && x.IsVisible).ToListAsync();
        return Results.Ok(new {
            totalCourses = ids.Count,
            publishedCourses = await db.Courses.CountAsync(x => ids.Contains(x.Id) && x.Status == CourseStatus.Published),
            enrollments,
            completions,
            completionRatePercent = enrollments == 0 ? 0m : Math.Round(completions * 100m / enrollments, 2),
            certificates = await db.Certificates.CountAsync(x => ids.Contains(x.CourseId)),
            reviews = reviews.Count,
            averageRating = reviews.Count == 0 ? 0m : Math.Round((decimal)reviews.Average(x => x.Rating), 2),
            favorites = await db.Set<Favorite>().CountAsync(x => ids.Contains(x.CourseId))
        });
    }

    private static async Task<IResult> GetAdminAsync(KogniaDbContext db, UserManager<ApplicationUser> userManager)
    {
        var enrollments = await db.Enrollments.CountAsync();
        var completions = await db.Enrollments.CountAsync(x => x.CompletedAtUtc != null);
        var reviews = await db.Set<Review>().Where(x => x.IsVisible).ToListAsync();
        var paid = await db.Payments.Where(x => x.Status == PaymentStatus.Paid).ToListAsync();
        return Results.Ok(new {
            totalUsers = await userManager.Users.CountAsync(),
            courses = await db.Courses.CountAsync(),
            publishedCourses = await db.Courses.CountAsync(x => x.Status == CourseStatus.Published),
            enrollments,
            completions,
            completionRatePercent = enrollments == 0 ? 0m : Math.Round(completions * 100m / enrollments, 2),
            activeSubscriptions = await db.Subscriptions.CountAsync(x => x.Status == SubscriptionStatus.Active),
            certificates = await db.Certificates.CountAsync(),
            reviews = reviews.Count,
            averageRating = reviews.Count == 0 ? 0m : Math.Round((decimal)reviews.Average(x => x.Rating), 2),
            favorites = await db.Set<Favorite>().CountAsync(),
            revenueByCurrency = paid.GroupBy(x => x.Currency).Select(g => new { currency = g.Key, amount = g.Sum(x => x.Amount) })
        });
    }
}
