using System.Security.Claims;
using Kognia.Domain.Catalog;
using Kognia.Domain.Engagement;
using Kognia.Domain.Learning;
using Kognia.Infrastructure.Identity;
using Kognia.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Kognia.Api.Endpoints;

public static class Block4Endpoints
{
    public static IEndpointRouteBuilder MapBlock4Endpoints(this IEndpointRouteBuilder app)
    {
        var engagement = app.MapGroup("/api/engagement").WithTags("Engagement");
        engagement.MapGet("/courses/{courseId:guid}/reviews", GetCourseReviewsAsync);
        engagement.MapPost("/courses/{courseId:guid}/reviews", UpsertReviewAsync).RequireAuthorization();
        engagement.MapDelete("/courses/{courseId:guid}/reviews/me", DeleteOwnReviewAsync).RequireAuthorization();
        engagement.MapGet("/favorites", GetFavoritesAsync).RequireAuthorization();
        engagement.MapPost("/favorites/{courseId:guid}", AddFavoriteAsync).RequireAuthorization();
        engagement.MapDelete("/favorites/{courseId:guid}", RemoveFavoriteAsync).RequireAuthorization();

        var notifications = app.MapGroup("/api/notifications").WithTags("Notifications").RequireAuthorization();
        notifications.MapGet("/", GetNotificationsAsync);
        notifications.MapPost("/{notificationId:guid}/read", MarkNotificationReadAsync);
        notifications.MapPost("/read-all", MarkAllNotificationsReadAsync);

        var admin = app.MapGroup("/api/admin").WithTags("Admin")
            .RequireAuthorization(policy => policy.RequireRole("Administrator"));
        admin.MapGet("/dashboard", GetAdminDashboardAsync);
        admin.MapGet("/users", GetUsersAsync);
        admin.MapPut("/users/{userId}/role", UpdateUserRoleAsync);
        admin.MapGet("/courses", GetAdminCoursesAsync);
        admin.MapPut("/courses/{courseId:guid}/status", UpdateCourseStatusAsync);
        admin.MapGet("/reviews", GetAdminReviewsAsync);
        admin.MapPut("/reviews/{reviewId:guid}/visibility", UpdateReviewVisibilityAsync);
        admin.MapPost("/notifications/broadcast", BroadcastNotificationAsync);

        return app;
    }

    private static string? GetUserId(ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.NameIdentifier);

    private static async Task<IResult> GetCourseReviewsAsync(Guid courseId, KogniaDbContext db, UserManager<ApplicationUser> userManager)
    {
        var reviews = await db.Set<Review>().AsNoTracking()
            .Where(x => x.CourseId == courseId && x.IsVisible)
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ToListAsync();

        var userIds = reviews.Select(x => x.StudentUserId).Distinct().ToArray();
        var users = await userManager.Users.AsNoTracking()
            .Where(x => userIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => $"{x.FirstName} {x.LastName}".Trim());

        var payload = reviews.Select(x => new
        {
            x.Id,
            x.Rating,
            x.Comment,
            x.CreatedAtUtc,
            x.UpdatedAtUtc,
            StudentName = users.GetValueOrDefault(x.StudentUserId, "Estudiante")
        });

        return Results.Ok(payload);
    }

    private static async Task<IResult> UpsertReviewAsync(Guid courseId, ReviewRequest request, ClaimsPrincipal principal, KogniaDbContext db)
    {
        var userId = GetUserId(principal);
        if (userId is null) return Results.Unauthorized();
        if (request.Rating is < 1 or > 5) return Results.BadRequest(new { message = "Rating must be between 1 and 5." });
        if (request.Comment?.Length > 2000) return Results.BadRequest(new { message = "Comment is too long." });

        var enrollment = await db.Enrollments.AsNoTracking()
            .AnyAsync(x => x.StudentUserId == userId && x.CourseId == courseId);
        if (!enrollment) return Results.BadRequest(new { message = "Only enrolled students can review this course." });

        var review = await db.Set<Review>().SingleOrDefaultAsync(x => x.StudentUserId == userId && x.CourseId == courseId);
        if (review is null)
        {
            review = new Review
            {
                StudentUserId = userId,
                CourseId = courseId,
                Rating = request.Rating,
                Comment = request.Comment?.Trim() ?? string.Empty
            };
            db.Set<Review>().Add(review);
        }
        else
        {
            review.Rating = request.Rating;
            review.Comment = request.Comment?.Trim() ?? string.Empty;
            review.IsVisible = true;
            review.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        var instructorUserId = await db.Courses.Where(x => x.Id == courseId).Select(x => x.InstructorUserId).SingleOrDefaultAsync();
        if (instructorUserId is not null)
        {
            db.Set<Notification>().Add(new Notification
            {
                UserId = instructorUserId,
                Type = NotificationType.Review,
                Title = "Nueva reseña en tu curso",
                Message = $"Un estudiante calificó tu curso con {request.Rating}/5.",
                ActionUrl = $"/instructor"
            });
        }

        await db.SaveChangesAsync();
        return Results.Ok(new { review.Id, review.Rating, review.Comment, review.IsVisible });
    }

    private static async Task<IResult> DeleteOwnReviewAsync(Guid courseId, ClaimsPrincipal principal, KogniaDbContext db)
    {
        var userId = GetUserId(principal);
        if (userId is null) return Results.Unauthorized();
        var review = await db.Set<Review>().SingleOrDefaultAsync(x => x.StudentUserId == userId && x.CourseId == courseId);
        if (review is null) return Results.NotFound();
        db.Set<Review>().Remove(review);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> GetFavoritesAsync(ClaimsPrincipal principal, KogniaDbContext db)
    {
        var userId = GetUserId(principal);
        if (userId is null) return Results.Unauthorized();
        var favorites = await db.Set<Favorite>().AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new { x.Course.Id, x.Course.Title, x.Course.Slug, x.Course.Summary, x.Course.Level, x.CreatedAtUtc })
            .ToListAsync();
        return Results.Ok(favorites);
    }

    private static async Task<IResult> AddFavoriteAsync(Guid courseId, ClaimsPrincipal principal, KogniaDbContext db)
    {
        var userId = GetUserId(principal);
        if (userId is null) return Results.Unauthorized();
        if (!await db.Courses.AnyAsync(x => x.Id == courseId && x.Status == CourseStatus.Published)) return Results.NotFound();
        if (!await db.Set<Favorite>().AnyAsync(x => x.UserId == userId && x.CourseId == courseId))
        {
            db.Set<Favorite>().Add(new Favorite { UserId = userId, CourseId = courseId });
            await db.SaveChangesAsync();
        }
        return Results.NoContent();
    }

    private static async Task<IResult> RemoveFavoriteAsync(Guid courseId, ClaimsPrincipal principal, KogniaDbContext db)
    {
        var userId = GetUserId(principal);
        if (userId is null) return Results.Unauthorized();
        var favorite = await db.Set<Favorite>().SingleOrDefaultAsync(x => x.UserId == userId && x.CourseId == courseId);
        if (favorite is null) return Results.NoContent();
        db.Set<Favorite>().Remove(favorite);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> GetNotificationsAsync(ClaimsPrincipal principal, KogniaDbContext db, bool unreadOnly = false)
    {
        var userId = GetUserId(principal);
        if (userId is null) return Results.Unauthorized();
        var query = db.Set<Notification>().AsNoTracking().Where(x => x.UserId == userId);
        if (unreadOnly) query = query.Where(x => !x.IsRead);
        var items = await query.OrderByDescending(x => x.CreatedAtUtc).Take(50).ToListAsync();
        return Results.Ok(new { unreadCount = items.Count(x => !x.IsRead), items });
    }

    private static async Task<IResult> MarkNotificationReadAsync(Guid notificationId, ClaimsPrincipal principal, KogniaDbContext db)
    {
        var userId = GetUserId(principal);
        if (userId is null) return Results.Unauthorized();
        var notification = await db.Set<Notification>().SingleOrDefaultAsync(x => x.Id == notificationId && x.UserId == userId);
        if (notification is null) return Results.NotFound();
        notification.IsRead = true;
        notification.ReadAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> MarkAllNotificationsReadAsync(ClaimsPrincipal principal, KogniaDbContext db)
    {
        var userId = GetUserId(principal);
        if (userId is null) return Results.Unauthorized();
        var unread = await db.Set<Notification>().Where(x => x.UserId == userId && !x.IsRead).ToListAsync();
        var now = DateTimeOffset.UtcNow;
        foreach (var item in unread) { item.IsRead = true; item.ReadAtUtc = now; }
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> GetAdminDashboardAsync(KogniaDbContext db, UserManager<ApplicationUser> userManager)
    {
        var users = await userManager.Users.CountAsync();
        var courses = await db.Courses.CountAsync();
        var publishedCourses = await db.Courses.CountAsync(x => x.Status == CourseStatus.Published);
        var enrollments = await db.Enrollments.CountAsync();
        var reviews = await db.Set<Review>().CountAsync();
        var visibleReviews = await db.Set<Review>().CountAsync(x => x.IsVisible);
        var unreadNotifications = await db.Set<Notification>().CountAsync(x => !x.IsRead);
        var certificates = await db.Certificates.CountAsync();
        return Results.Ok(new { users, courses, publishedCourses, enrollments, reviews, visibleReviews, unreadNotifications, certificates });
    }

    private static async Task<IResult> GetUsersAsync(UserManager<ApplicationUser> userManager)
    {
        var users = await userManager.Users.AsNoTracking().OrderBy(x => x.Email).Take(200).ToListAsync();
        var result = new List<object>();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            result.Add(new { user.Id, user.Email, user.FirstName, user.LastName, user.EmailConfirmed, Roles = roles });
        }
        return Results.Ok(result);
    }

    private static async Task<IResult> UpdateUserRoleAsync(string userId, RoleRequest request, UserManager<ApplicationUser> userManager)
    {
        var allowed = new[] { "Student", "Instructor", "Administrator" };
        if (!allowed.Contains(request.Role, StringComparer.OrdinalIgnoreCase)) return Results.BadRequest(new { message = "Unsupported role." });
        var canonicalRole = allowed.Single(x => x.Equals(request.Role, StringComparison.OrdinalIgnoreCase));
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return Results.NotFound();
        var currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0) await userManager.RemoveFromRolesAsync(user, currentRoles);
        var result = await userManager.AddToRoleAsync(user, canonicalRole);
        return result.Succeeded ? Results.NoContent() : Results.BadRequest(result.Errors);
    }

    private static async Task<IResult> GetAdminCoursesAsync(KogniaDbContext db)
    {
        var courses = await db.Courses.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new { x.Id, x.Title, x.Slug, x.Status, x.InstructorUserId, Category = x.Category.Name, x.CreatedAtUtc, x.PublishedAtUtc })
            .ToListAsync();
        return Results.Ok(courses);
    }

    private static async Task<IResult> UpdateCourseStatusAsync(Guid courseId, CourseStatusRequest request, KogniaDbContext db)
    {
        if (!Enum.IsDefined(typeof(CourseStatus), request.Status)) return Results.BadRequest(new { message = "Invalid course status." });
        var course = await db.Courses.SingleOrDefaultAsync(x => x.Id == courseId);
        if (course is null) return Results.NotFound();
        course.Status = (CourseStatus)request.Status;
        course.PublishedAtUtc = course.Status == CourseStatus.Published ? course.PublishedAtUtc ?? DateTimeOffset.UtcNow : course.PublishedAtUtc;
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> GetAdminReviewsAsync(KogniaDbContext db)
    {
        var reviews = await db.Set<Review>().AsNoTracking().OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new { x.Id, x.CourseId, CourseTitle = x.Course.Title, x.StudentUserId, x.Rating, x.Comment, x.IsVisible, x.CreatedAtUtc })
            .ToListAsync();
        return Results.Ok(reviews);
    }

    private static async Task<IResult> UpdateReviewVisibilityAsync(Guid reviewId, VisibilityRequest request, KogniaDbContext db)
    {
        var review = await db.Set<Review>().SingleOrDefaultAsync(x => x.Id == reviewId);
        if (review is null) return Results.NotFound();
        review.IsVisible = request.IsVisible;
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> BroadcastNotificationAsync(BroadcastNotificationRequest request, UserManager<ApplicationUser> userManager, KogniaDbContext db)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Message))
            return Results.BadRequest(new { message = "Title and message are required." });
        var userIds = await userManager.Users.Select(x => x.Id).ToListAsync();
        foreach (var userId in userIds)
        {
            db.Set<Notification>().Add(new Notification
            {
                UserId = userId,
                Type = NotificationType.Administration,
                Title = request.Title.Trim(),
                Message = request.Message.Trim(),
                ActionUrl = request.ActionUrl
            });
        }
        await db.SaveChangesAsync();
        return Results.Ok(new { recipients = userIds.Count });
    }

    public sealed record ReviewRequest(int Rating, string? Comment);
    public sealed record RoleRequest(string Role);
    public sealed record CourseStatusRequest(int Status);
    public sealed record VisibilityRequest(bool IsVisible);
    public sealed record BroadcastNotificationRequest(string Title, string Message, string? ActionUrl);
}
