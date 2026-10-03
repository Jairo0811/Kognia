using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Kognia.Domain.Catalog;
using Kognia.Domain.Learning;
using Kognia.Infrastructure.Identity;
using Kognia.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kognia.Api.IntegrationTests;

public sealed class Block4ApiTests(KogniaApiFactory factory) : IClassFixture<KogniaApiFactory>
{
    [Fact]
    public async Task Enrolled_student_can_favorite_and_review_course()
    {
        var (studentId, studentToken) = await CreateUserAsync("Student");
        var (instructorId, _) = await CreateUserAsync("Instructor");
        var courseId = await SeedCourseAndEnrollmentAsync(studentId, instructorId);
        using var student = Authorized(studentToken);

        Assert.Equal(HttpStatusCode.NoContent, (await student.PostAsync($"/api/engagement/favorites/{courseId}", null)).StatusCode);
        var favorites = await student.GetAsync("/api/engagement/favorites");
        favorites.EnsureSuccessStatusCode();
        using var favoritesJson = JsonDocument.Parse(await favorites.Content.ReadAsStringAsync());
        Assert.Contains(favoritesJson.RootElement.EnumerateArray(), x => x.GetProperty("id").GetGuid() == courseId);

        var review = await student.PostAsJsonAsync($"/api/engagement/courses/{courseId}/reviews", new { rating = 5, comment = "Excelente curso" });
        review.EnsureSuccessStatusCode();

        using var anonymous = factory.CreateClient();
        var publicReviews = await anonymous.GetAsync($"/api/engagement/courses/{courseId}/reviews");
        publicReviews.EnsureSuccessStatusCode();
        using var reviewsJson = JsonDocument.Parse(await publicReviews.Content.ReadAsStringAsync());
        Assert.Contains(reviewsJson.RootElement.EnumerateArray(), x => x.GetProperty("rating").GetInt32() == 5);
    }

    [Fact]
    public async Task Review_creates_instructor_notification_and_it_can_be_marked_read()
    {
        var (studentId, studentToken) = await CreateUserAsync("Student");
        var (instructorId, instructorToken) = await CreateUserAsync("Instructor");
        var courseId = await SeedCourseAndEnrollmentAsync(studentId, instructorId);
        using var student = Authorized(studentToken);
        await student.PostAsJsonAsync($"/api/engagement/courses/{courseId}/reviews", new { rating = 4, comment = "Muy bueno" });

        using var instructor = Authorized(instructorToken);
        var notifications = await instructor.GetAsync("/api/notifications/");
        notifications.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await notifications.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("unreadCount").GetInt32() >= 1);
        var notificationId = json.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await instructor.PostAsync($"/api/notifications/{notificationId}/read", null)).StatusCode);
    }

    [Fact]
    public async Task Admin_routes_require_admin_and_admin_can_broadcast()
    {
        var (_, studentToken) = await CreateUserAsync("Student");
        var (_, adminToken) = await CreateUserAsync("Administrator");
        using var student = Authorized(studentToken);
        using var admin = Authorized(adminToken);

        Assert.Equal(HttpStatusCode.Forbidden, (await student.GetAsync("/api/admin/dashboard")).StatusCode);
        (await admin.GetAsync("/api/admin/dashboard")).EnsureSuccessStatusCode();
        (await admin.GetAsync("/api/admin/users")).EnsureSuccessStatusCode();
        (await admin.GetAsync("/api/admin/courses")).EnsureSuccessStatusCode();

        var broadcast = await admin.PostAsJsonAsync("/api/admin/notifications/broadcast", new { title = "Aviso", message = "Mensaje administrativo", actionUrl = "/" });
        broadcast.EnsureSuccessStatusCode();
        using var broadcastJson = JsonDocument.Parse(await broadcast.Content.ReadAsStringAsync());
        Assert.True(broadcastJson.RootElement.GetProperty("recipients").GetInt32() >= 2);
    }

    private HttpClient Authorized(string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<(string UserId, string Token)> CreateUserAsync(string role)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<JwtTokenService>();
        var email = $"block4-{role.ToLowerInvariant()}-{Guid.NewGuid():N}@kognia.test";
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, FirstName = role, LastName = "Block4" };
        var created = await userManager.CreateAsync(user, "Kognia!2026");
        Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(x => x.Description)));
        var roleResult = await userManager.AddToRoleAsync(user, role);
        Assert.True(roleResult.Succeeded, string.Join("; ", roleResult.Errors.Select(x => x.Description)));
        var (token, _) = await tokenService.CreateAccessTokenAsync(user);
        return (user.Id, token);
    }

    private async Task<Guid> SeedCourseAndEnrollmentAsync(string studentId, string instructorId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KogniaDbContext>();
        var categoryId = await db.Categories.Select(x => x.Id).FirstAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var course = new Course
        {
            Title = $"Block 4 course {suffix}",
            Slug = $"block-4-{suffix}",
            Summary = "Engagement test",
            Description = "Block 4 integration test course",
            Level = "Beginner",
            InstructorUserId = instructorId,
            CategoryId = categoryId,
            Status = CourseStatus.Published,
            PublishedAtUtc = DateTimeOffset.UtcNow
        };
        db.Courses.Add(course);
        db.Enrollments.Add(new Enrollment { StudentUserId = studentId, Course = course });
        await db.SaveChangesAsync();
        return course.Id;
    }
}
