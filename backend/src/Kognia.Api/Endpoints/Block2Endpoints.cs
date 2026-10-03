using System.Security.Claims;
using Kognia.Domain.Assessments;
using Kognia.Domain.Catalog;
using Kognia.Domain.Certificates;
using Kognia.Domain.Learning;
using Kognia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kognia.Api.Endpoints;

public static class Block2Endpoints
{
    public static IEndpointRouteBuilder MapBlock2Endpoints(this IEndpointRouteBuilder app)
    {
        var learning = app.MapGroup("/api/learning").WithTags("Learning").RequireAuthorization();
        learning.MapPost("/courses/{courseId:guid}/enroll", EnrollAsync);
        learning.MapGet("/me/courses", GetMyCoursesAsync);
        learning.MapGet("/courses/{courseId:guid}", GetLearningCourseAsync);
        learning.MapPut("/lessons/{lessonId:guid}/progress", UpdateLessonProgressAsync);
        learning.MapGet("/courses/{courseId:guid}/quizzes", GetCourseQuizzesAsync);
        learning.MapGet("/quizzes/{quizId:guid}", GetQuizAsync);
        learning.MapPost("/quizzes/{quizId:guid}/attempts", SubmitQuizAsync);

        var instructor = app.MapGroup("/api/instructor").WithTags("Assessments")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Administrator"));
        instructor.MapPost("/courses/{courseId:guid}/quizzes", CreateQuizAsync);
        instructor.MapPost("/quizzes/{quizId:guid}/questions", AddQuestionAsync);
        instructor.MapPost("/quizzes/{quizId:guid}/publish", PublishQuizAsync);

        var certificates = app.MapGroup("/api/certificates").WithTags("Certificates");
        certificates.MapGet("/me", GetMyCertificatesAsync).RequireAuthorization();
        certificates.MapGet("/verify/{verificationCode}", VerifyCertificateAsync);

        return app;
    }

    private static string? GetUserId(ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.NameIdentifier);

    private static async Task<IResult> EnrollAsync(Guid courseId, ClaimsPrincipal principal, KogniaDbContext db)
    {
        var userId = GetUserId(principal);
        if (userId is null) return Results.Unauthorized();

        var course = await db.Courses.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == courseId && x.Status == CourseStatus.Published);
        if (course is null) return Results.NotFound(new { message = "Published course not found." });

        var existing = await db.Enrollments.SingleOrDefaultAsync(x => x.StudentUserId == userId && x.CourseId == courseId);
        if (existing is not null)
            return Results.Ok(new { existing.Id, existing.CourseId, existing.Status, existing.EnrolledAtUtc });

        var enrollment = new Enrollment { StudentUserId = userId, CourseId = courseId };
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync();

        return Results.Created($"/api/learning/courses/{courseId}", new
        {
            enrollment.Id,
            enrollment.CourseId,
            enrollment.Status,
            enrollment.EnrolledAtUtc
        });
    }

    private static async Task<IResult> GetMyCoursesAsync(ClaimsPrincipal principal, KogniaDbContext db)
    {
        var userId = GetUserId(principal);
        if (userId is null) return Results.Unauthorized();

        var courses = await db.Enrollments.AsNoTracking()
            .Where(x => x.StudentUserId == userId)
            .OrderByDescending(x => x.EnrolledAtUtc)
            .Select(x => new
            {
                EnrollmentId = x.Id,
                x.CourseId,
                x.Course.Title,
                x.Course.Slug,
                x.Course.Summary,
                x.Course.Level,
                x.Status,
                x.EnrolledAtUtc,
                x.CompletedAtUtc,
                TotalLessons = x.Course.Sections.SelectMany(s => s.Lessons).Count(),
                CompletedLessons = x.LessonProgress.Count(p => p.IsCompleted)
            })
            .ToListAsync();

        var result = courses.Select(x => new
        {
            x.EnrollmentId,
            x.CourseId,
            x.Title,
            x.Slug,
            x.Summary,
            x.Level,
            x.Status,
            x.EnrolledAtUtc,
            x.CompletedAtUtc,
            x.TotalLessons,
            x.CompletedLessons,
            ProgressPercent = x.TotalLessons == 0 ? 0 : (int)Math.Round(x.CompletedLessons * 100d / x.TotalLessons)
        });

        return Results.Ok(result);
    }

    private static async Task<IResult> GetLearningCourseAsync(Guid courseId, ClaimsPrincipal principal, KogniaDbContext db)
    {
        var userId = GetUserId(principal);
        if (userId is null) return Results.Unauthorized();

        var enrollment = await db.Enrollments.AsNoTracking()
            .SingleOrDefaultAsync(x => x.StudentUserId == userId && x.CourseId == courseId);
        if (enrollment is null) return Results.Forbid();

        var course = await db.Courses.AsNoTracking()
            .Where(x => x.Id == courseId)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Slug,
                x.Summary,
                x.Description,
                x.Level,
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
                        l.Content,
                        l.VideoUrl,
                        l.Position,
                        l.IsPreview
                    })
                })
            })
            .SingleOrDefaultAsync();
        if (course is null) return Results.NotFound();

        var progress = await db.LessonProgress.AsNoTracking()
            .Where(x => x.EnrollmentId == enrollment.Id)
            .ToDictionaryAsync(x => x.LessonId, x => new
            {
                x.IsCompleted,
                x.LastPositionSeconds,
                x.LastAccessedAtUtc,
                x.CompletedAtUtc
            });

        var quizzes = await db.Quizzes.AsNoTracking()
            .Where(x => x.CourseId == courseId && x.IsPublished)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.PassingScorePercent,
                Questions = x.Questions.Count,
                BestScore = x.Attempts.Where(a => a.StudentUserId == userId)
                    .Select(a => (decimal?)a.ScorePercent).Max(),
                Passed = x.Attempts.Any(a => a.StudentUserId == userId && a.Passed)
            }).ToListAsync();

        return Results.Ok(new { course, enrollment = new { enrollment.Id, enrollment.Status, enrollment.EnrolledAtUtc, enrollment.CompletedAtUtc }, progress, quizzes });
    }

    private static async Task<IResult> UpdateLessonProgressAsync(
        Guid lessonId,
        LessonProgressRequest request,
        ClaimsPrincipal principal,
        KogniaDbContext db)
    {
        var userId = GetUserId(principal);
        if (userId is null) return Results.Unauthorized();

        var lesson = await db.Lessons.AsNoTracking()
            .Where(x => x.Id == lessonId)
            .Select(x => new { x.Id, CourseId = x.CourseSection.CourseId })
            .SingleOrDefaultAsync();
        if (lesson is null) return Results.NotFound();

        var enrollment = await db.Enrollments
            .SingleOrDefaultAsync(x => x.StudentUserId == userId && x.CourseId == lesson.CourseId);
        if (enrollment is null) return Results.Forbid();

        var progress = await db.LessonProgress
            .SingleOrDefaultAsync(x => x.EnrollmentId == enrollment.Id && x.LessonId == lessonId);
        if (progress is null)
        {
            progress = new LessonProgress { EnrollmentId = enrollment.Id, LessonId = lessonId };
            db.LessonProgress.Add(progress);
        }

        progress.LastPositionSeconds = Math.Max(0, request.LastPositionSeconds);
        progress.LastAccessedAtUtc = DateTimeOffset.UtcNow;
        progress.IsCompleted = request.IsCompleted;
        progress.CompletedAtUtc = request.IsCompleted ? progress.CompletedAtUtc ?? DateTimeOffset.UtcNow : null;

        await db.SaveChangesAsync();

        var totalLessons = await db.Lessons.CountAsync(x => x.CourseSection.CourseId == lesson.CourseId);
        var completedLessons = await db.LessonProgress.CountAsync(x => x.EnrollmentId == enrollment.Id && x.IsCompleted);
        var courseCompleted = totalLessons > 0 && completedLessons >= totalLessons;

        enrollment.Status = courseCompleted ? EnrollmentStatus.Completed : EnrollmentStatus.Active;
        enrollment.CompletedAtUtc = courseCompleted ? enrollment.CompletedAtUtc ?? DateTimeOffset.UtcNow : null;
        await db.SaveChangesAsync();

        var certificate = await IssueCertificateIfEligibleAsync(userId, lesson.CourseId, db);
        var progressPercent = totalLessons == 0 ? 0 : (int)Math.Round(completedLessons * 100d / totalLessons);

        return Results.Ok(new
        {
            lessonId,
            progress.IsCompleted,
            progress.LastPositionSeconds,
            completedLessons,
            totalLessons,
            progressPercent,
            courseCompleted,
            certificate = certificate is null ? null : new { certificate.Id, certificate.VerificationCode, certificate.IssuedAtUtc }
        });
    }

    private static async Task<IResult> GetCourseQuizzesAsync(Guid courseId, ClaimsPrincipal principal, KogniaDbContext db)
    {
        var userId = GetUserId(principal);
        if (userId is null) return Results.Unauthorized();
        if (!await db.Enrollments.AnyAsync(x => x.StudentUserId == userId && x.CourseId == courseId)) return Results.Forbid();

        var quizzes = await db.Quizzes.AsNoTracking()
            .Where(x => x.CourseId == courseId && x.IsPublished)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.PassingScorePercent,
                Questions = x.Questions.Count,
                Attempts = x.Attempts.Count(a => a.StudentUserId == userId),
                BestScore = x.Attempts.Where(a => a.StudentUserId == userId).Select(a => (decimal?)a.ScorePercent).Max(),
                Passed = x.Attempts.Any(a => a.StudentUserId == userId && a.Passed)
            }).ToListAsync();
        return Results.Ok(quizzes);
    }

    private static async Task<IResult> GetQuizAsync(Guid quizId, ClaimsPrincipal principal, KogniaDbContext db)
    {
        var userId = GetUserId(principal);
        if (userId is null) return Results.Unauthorized();

        var quiz = await db.Quizzes.AsNoTracking()
            .Where(x => x.Id == quizId && x.IsPublished)
            .Select(x => new
            {
                x.Id,
                x.CourseId,
                x.Title,
                x.PassingScorePercent,
                Questions = x.Questions.OrderBy(q => q.Position).Select(q => new
                {
                    q.Id,
                    q.Text,
                    q.Position,
                    Options = q.Options.OrderBy(o => o.Position).Select(o => new { o.Id, o.Text, o.Position })
                })
            }).SingleOrDefaultAsync();
        if (quiz is null) return Results.NotFound();
        if (!await db.Enrollments.AnyAsync(x => x.StudentUserId == userId && x.CourseId == quiz.CourseId)) return Results.Forbid();

        return Results.Ok(quiz);
    }

    private static async Task<IResult> SubmitQuizAsync(
        Guid quizId,
        SubmitQuizRequest request,
        ClaimsPrincipal principal,
        KogniaDbContext db)
    {
        var userId = GetUserId(principal);
        if (userId is null) return Results.Unauthorized();

        var quiz = await db.Quizzes.Include(x => x.Questions).ThenInclude(x => x.Options)
            .SingleOrDefaultAsync(x => x.Id == quizId && x.IsPublished);
        if (quiz is null) return Results.NotFound();
        if (!await db.Enrollments.AnyAsync(x => x.StudentUserId == userId && x.CourseId == quiz.CourseId)) return Results.Forbid();
        if (quiz.Questions.Count == 0) return Results.BadRequest(new { message = "Quiz has no questions." });

        var submitted = request.Answers.ToDictionary(x => x.QuestionId, x => x.SelectedOptionId);
        var attempt = new QuizAttempt { QuizId = quiz.Id, StudentUserId = userId };
        var correct = 0;

        foreach (var question in quiz.Questions)
        {
            submitted.TryGetValue(question.Id, out var selectedOptionId);
            var selected = question.Options.SingleOrDefault(x => x.Id == selectedOptionId);
            var isCorrect = selected?.IsCorrect == true;
            if (isCorrect) correct++;

            attempt.Answers.Add(new QuizAnswer
            {
                QuestionId = question.Id,
                SelectedOptionId = selected?.Id,
                IsCorrect = isCorrect
            });
        }

        attempt.ScorePercent = Math.Round(correct * 100m / quiz.Questions.Count, 2);
        attempt.Passed = attempt.ScorePercent >= quiz.PassingScorePercent;
        db.QuizAttempts.Add(attempt);
        await db.SaveChangesAsync();

        var certificate = await IssueCertificateIfEligibleAsync(userId, quiz.CourseId, db);

        return Results.Ok(new
        {
            attempt.Id,
            attempt.ScorePercent,
            attempt.Passed,
            correctAnswers = correct,
            totalQuestions = quiz.Questions.Count,
            attempt.SubmittedAtUtc,
            certificate = certificate is null ? null : new { certificate.Id, certificate.VerificationCode, certificate.IssuedAtUtc }
        });
    }

    private static async Task<IResult> CreateQuizAsync(
        Guid courseId,
        CreateQuizRequest request,
        ClaimsPrincipal principal,
        KogniaDbContext db)
    {
        var course = await db.Courses.FindAsync(courseId);
        if (course is null) return Results.NotFound();
        if (!CanManage(principal, course)) return Results.Forbid();
        if (request.PassingScorePercent is < 1 or > 100)
            return Results.BadRequest(new { message = "Passing score must be between 1 and 100." });

        var quiz = new Quiz
        {
            CourseId = courseId,
            Title = request.Title.Trim(),
            PassingScorePercent = request.PassingScorePercent
        };
        db.Quizzes.Add(quiz);
        await db.SaveChangesAsync();
        return Results.Created($"/api/instructor/quizzes/{quiz.Id}", new { quiz.Id, quiz.Title, quiz.PassingScorePercent, quiz.IsPublished });
    }

    private static async Task<IResult> AddQuestionAsync(
        Guid quizId,
        CreateQuestionRequest request,
        ClaimsPrincipal principal,
        KogniaDbContext db)
    {
        var quiz = await db.Quizzes.Include(x => x.Course).SingleOrDefaultAsync(x => x.Id == quizId);
        if (quiz is null) return Results.NotFound();
        if (!CanManage(principal, quiz.Course)) return Results.Forbid();
        if (request.Options.Count < 2 || !request.Options.Any(x => x.IsCorrect))
            return Results.BadRequest(new { message = "A question requires at least two options and one correct option." });

        var question = new Question
        {
            QuizId = quizId,
            Text = request.Text.Trim(),
            Position = request.Position
        };
        foreach (var option in request.Options)
        {
            question.Options.Add(new QuestionOption
            {
                Text = option.Text.Trim(),
                IsCorrect = option.IsCorrect,
                Position = option.Position
            });
        }

        db.Questions.Add(question);
        await db.SaveChangesAsync();
        return Results.Created($"/api/instructor/quizzes/{quizId}/questions/{question.Id}", new { question.Id, question.Text, question.Position });
    }

    private static async Task<IResult> PublishQuizAsync(Guid quizId, ClaimsPrincipal principal, KogniaDbContext db)
    {
        var quiz = await db.Quizzes.Include(x => x.Course).Include(x => x.Questions).ThenInclude(x => x.Options)
            .SingleOrDefaultAsync(x => x.Id == quizId);
        if (quiz is null) return Results.NotFound();
        if (!CanManage(principal, quiz.Course)) return Results.Forbid();
        if (quiz.Questions.Count == 0 || quiz.Questions.Any(q => q.Options.Count < 2 || !q.Options.Any(o => o.IsCorrect)))
            return Results.BadRequest(new { message = "Quiz requires valid questions before publishing." });

        quiz.IsPublished = true;
        await db.SaveChangesAsync();
        return Results.Ok(new { quiz.Id, quiz.IsPublished });
    }

    private static async Task<IResult> GetMyCertificatesAsync(ClaimsPrincipal principal, KogniaDbContext db)
    {
        var userId = GetUserId(principal);
        if (userId is null) return Results.Unauthorized();

        var certificates = await db.Certificates.AsNoTracking()
            .Where(x => x.StudentUserId == userId)
            .OrderByDescending(x => x.IssuedAtUtc)
            .Select(x => new { x.Id, x.CourseId, CourseTitle = x.Course.Title, x.VerificationCode, x.IssuedAtUtc })
            .ToListAsync();
        return Results.Ok(certificates);
    }

    private static async Task<IResult> VerifyCertificateAsync(string verificationCode, KogniaDbContext db)
    {
        var certificate = await db.Certificates.AsNoTracking()
            .Where(x => x.VerificationCode == verificationCode)
            .Select(x => new
            {
                x.Id,
                x.VerificationCode,
                x.IssuedAtUtc,
                x.CourseId,
                CourseTitle = x.Course.Title,
                Student = db.Users.Where(u => u.Id == x.StudentUserId)
                    .Select(u => new { u.FirstName, u.LastName }).SingleOrDefault()
            }).SingleOrDefaultAsync();

        return certificate is null
            ? Results.NotFound(new { valid = false, message = "Certificate not found." })
            : Results.Ok(new { valid = true, certificate });
    }

    private static async Task<Certificate?> IssueCertificateIfEligibleAsync(string studentUserId, Guid courseId, KogniaDbContext db)
    {
        var existing = await db.Certificates.SingleOrDefaultAsync(x => x.StudentUserId == studentUserId && x.CourseId == courseId);
        if (existing is not null) return existing;

        var completed = await db.Enrollments.AnyAsync(x =>
            x.StudentUserId == studentUserId && x.CourseId == courseId && x.Status == EnrollmentStatus.Completed);
        if (!completed) return null;

        var requiredQuizIds = await db.Quizzes.Where(x => x.CourseId == courseId && x.IsPublished).Select(x => x.Id).ToListAsync();
        if (requiredQuizIds.Count > 0)
        {
            var passedQuizIds = await db.QuizAttempts
                .Where(x => x.StudentUserId == studentUserId && x.Passed && requiredQuizIds.Contains(x.QuizId))
                .Select(x => x.QuizId).Distinct().ToListAsync();
            if (passedQuizIds.Count != requiredQuizIds.Count) return null;
        }

        var certificate = new Certificate
        {
            StudentUserId = studentUserId,
            CourseId = courseId,
            VerificationCode = $"KOG-{DateTime.UtcNow:yyyy}-{Guid.NewGuid():N}"[..25].ToUpperInvariant()
        };
        db.Certificates.Add(certificate);
        await db.SaveChangesAsync();
        return certificate;
    }

    private static bool CanManage(ClaimsPrincipal principal, Course course) =>
        principal.IsInRole("Administrator") || course.InstructorUserId == GetUserId(principal);

    public sealed record LessonProgressRequest(int LastPositionSeconds, bool IsCompleted);
    public sealed record CreateQuizRequest(string Title, int PassingScorePercent);
    public sealed record CreateQuestionRequest(string Text, int Position, IReadOnlyCollection<CreateQuestionOptionRequest> Options);
    public sealed record CreateQuestionOptionRequest(string Text, bool IsCorrect, int Position);
    public sealed record SubmitQuizRequest(IReadOnlyCollection<QuizAnswerRequest> Answers);
    public sealed record QuizAnswerRequest(Guid QuestionId, Guid? SelectedOptionId);
}
