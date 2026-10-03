using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Kognia.Domain.Catalog;
using Kognia.Infrastructure.Identity;
using Kognia.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kognia.Api.IntegrationTests;

public sealed class Block2ApiTests(KogniaApiFactory factory) : IClassFixture<KogniaApiFactory>
{
    [Fact]
    public async Task Learning_completion_issues_certificate_when_no_quizzes_are_required()
    {
        var (studentId, token) = await CreateUserAsync("Student");
        var courseId = await SeedPublishedCourseAsync("Learning flow", studentId);
        using var client = CreateAuthorizedClient(token);

        var enroll = await client.PostAsync($"/api/learning/courses/{courseId}/enroll", null);
        Assert.Equal(HttpStatusCode.Created, enroll.StatusCode);

        var course = await client.GetAsync($"/api/learning/courses/{courseId}");
        course.EnsureSuccessStatusCode();
        using var courseJson = JsonDocument.Parse(await course.Content.ReadAsStringAsync());
        var lessonId = courseJson.RootElement.GetProperty("course").GetProperty("sections")[0]
            .GetProperty("lessons")[0].GetProperty("id").GetGuid();

        var progress = await client.PutAsJsonAsync($"/api/learning/lessons/{lessonId}/progress", new
        {
            lastPositionSeconds = 120,
            isCompleted = true
        });
        progress.EnsureSuccessStatusCode();

        using var progressJson = JsonDocument.Parse(await progress.Content.ReadAsStringAsync());
        Assert.True(progressJson.RootElement.GetProperty("courseCompleted").GetBoolean());
        var certificate = progressJson.RootElement.GetProperty("certificate");
        Assert.Equal(JsonValueKind.Object, certificate.ValueKind);
        var verificationCode = certificate.GetProperty("verificationCode").GetString();
        Assert.False(string.IsNullOrWhiteSpace(verificationCode));

        var certificates = await client.GetAsync("/api/certificates/me");
        certificates.EnsureSuccessStatusCode();
        using var certificatesJson = JsonDocument.Parse(await certificates.Content.ReadAsStringAsync());
        Assert.Single(certificatesJson.RootElement.EnumerateArray());

        using var anonymous = factory.CreateClient();
        var verify = await anonymous.GetAsync($"/api/certificates/verify/{verificationCode}");
        verify.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Instructor_can_publish_quiz_and_student_attempt_is_scored()
    {
        var (instructorId, instructorToken) = await CreateUserAsync("Instructor");
        var (_, studentToken) = await CreateUserAsync("Student");
        var courseId = await SeedPublishedCourseAsync("Assessment flow", instructorId);

        using var instructor = CreateAuthorizedClient(instructorToken);
        var createQuiz = await instructor.PostAsJsonAsync($"/api/instructor/courses/{courseId}/quizzes", new
        {
            title = "Final quiz",
            passingScorePercent = 70
        });
        Assert.Equal(HttpStatusCode.Created, createQuiz.StatusCode);
        using var quizCreatedJson = JsonDocument.Parse(await createQuiz.Content.ReadAsStringAsync());
        var quizId = quizCreatedJson.RootElement.GetProperty("id").GetGuid();

        var addQuestion = await instructor.PostAsJsonAsync($"/api/instructor/quizzes/{quizId}/questions", new
        {
            text = "Which option is correct?",
            position = 1,
            options = new[]
            {
                new { text = "Correct", isCorrect = true, position = 1 },
                new { text = "Incorrect", isCorrect = false, position = 2 }
            }
        });
        Assert.Equal(HttpStatusCode.Created, addQuestion.StatusCode);

        var instructorQuiz = await instructor.GetAsync($"/api/instructor/quizzes/{quizId}");
        instructorQuiz.EnsureSuccessStatusCode();
        using var instructorQuizJson = JsonDocument.Parse(await instructorQuiz.Content.ReadAsStringAsync());
        var question = instructorQuizJson.RootElement.GetProperty("questions")[0];
        var questionId = question.GetProperty("id").GetGuid();
        var correctOptionId = question.GetProperty("options").EnumerateArray()
            .Single(x => x.GetProperty("isCorrect").GetBoolean()).GetProperty("id").GetGuid();

        var publish = await instructor.PostAsync($"/api/instructor/quizzes/{quizId}/publish", null);
        publish.EnsureSuccessStatusCode();

        using var student = CreateAuthorizedClient(studentToken);
        var enroll = await student.PostAsync($"/api/learning/courses/{courseId}/enroll", null);
        Assert.Equal(HttpStatusCode.Created, enroll.StatusCode);

        var studentQuiz = await student.GetAsync($"/api/learning/quizzes/{quizId}");
        studentQuiz.EnsureSuccessStatusCode();
        var studentQuizBody = await studentQuiz.Content.ReadAsStringAsync();
        Assert.DoesNotContain("isCorrect", studentQuizBody, StringComparison.OrdinalIgnoreCase);

        var attempt = await student.PostAsJsonAsync($"/api/learning/quizzes/{quizId}/attempts", new
        {
            answers = new[] { new { questionId, selectedOptionId = correctOptionId } }
        });
        attempt.EnsureSuccessStatusCode();
        using var attemptJson = JsonDocument.Parse(await attempt.Content.ReadAsStringAsync());
        Assert.True(attemptJson.RootElement.GetProperty("passed").GetBoolean());
        Assert.Equal(100m, attemptJson.RootElement.GetProperty("scorePercent").GetDecimal());
    }

    [Fact]
    public async Task Learning_routes_require_authentication_and_certificate_verification_is_public()
    {
        using var client = factory.CreateClient();
        var learning = await client.GetAsync("/api/learning/me/courses");
        Assert.Equal(HttpStatusCode.Unauthorized, learning.StatusCode);

        var verify = await client.GetAsync("/api/certificates/verify/NOT-A-REAL-CERTIFICATE");
        Assert.Equal(HttpStatusCode.NotFound, verify.StatusCode);
    }

    private HttpClient CreateAuthorizedClient(string token)
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
        var email = $"{role.ToLowerInvariant()}-{Guid.NewGuid():N}@kognia.test";
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = role,
            LastName = "Integration"
        };

        var created = await userManager.CreateAsync(user, "Kognia!2026");
        Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(x => x.Description)));
        var roleResult = await userManager.AddToRoleAsync(user, role);
        Assert.True(roleResult.Succeeded, string.Join("; ", roleResult.Errors.Select(x => x.Description)));

        var (token, _) = await tokenService.CreateAccessTokenAsync(user);
        return (user.Id, token);
    }

    private async Task<Guid> SeedPublishedCourseAsync(string titlePrefix, string instructorUserId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KogniaDbContext>();
        var categoryId = await db.Categories.Select(x => x.Id).FirstAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var course = new Course
        {
            Title = $"{titlePrefix} {suffix}",
            Slug = $"{titlePrefix.Replace(' ', '-').ToLowerInvariant()}-{suffix}",
            Summary = "Integration course",
            Description = "Course used by Block 2 integration tests.",
            Level = "Beginner",
            InstructorUserId = instructorUserId,
            CategoryId = categoryId,
            Status = CourseStatus.Published,
            PublishedAtUtc = DateTimeOffset.UtcNow
        };
        course.Sections.Add(new CourseSection
        {
            Title = "Section 1",
            Position = 1,
            Lessons = new List<Lesson>
            {
                new() { Title = "Lesson 1", LessonType = "Text", Content = "Test content", Position = 1 }
            }
        });
        db.Courses.Add(course);
        await db.SaveChangesAsync();
        return course.Id;
    }
}
