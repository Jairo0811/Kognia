using System.Security.Claims;
using Kognia.Domain.Billing;
using Kognia.Domain.Learning;
using Kognia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kognia.Api.Endpoints;

public static class Block3Endpoints
{
    public static IEndpointRouteBuilder MapBlock3Endpoints(this IEndpointRouteBuilder app)
    {
        var billing = app.MapGroup("/api/billing").WithTags("Billing");
        billing.MapGet("/plans", GetPlansAsync);
        billing.MapGet("/me", GetBillingAsync).RequireAuthorization();
        billing.MapPost("/checkout", StartCheckoutAsync).RequireAuthorization();
        billing.MapPost("/subscription/cancel", CancelSubscriptionAsync).RequireAuthorization();
        billing.MapPost("/sandbox/checkout/{checkoutToken}/complete", CompleteSandboxCheckoutAsync).RequireAuthorization();

        var dashboard = app.MapGroup("/api/dashboard").WithTags("Dashboard").RequireAuthorization();
        dashboard.MapGet("/student", GetStudentDashboardAsync);
        dashboard.MapGet("/instructor", GetInstructorDashboardAsync)
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Administrator"));

        return app;
    }

    private static string? GetUserId(ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.NameIdentifier);

    private static async Task<IResult> GetPlansAsync(KogniaDbContext db)
    {
        var plans = await db.SubscriptionPlans.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.Description,
                x.BillingPeriod,
                x.PriceAmount,
                x.Currency
            })
            .ToListAsync();

        return Results.Ok(plans);
    }

    private static async Task<IResult> GetBillingAsync(ClaimsPrincipal principal, KogniaDbContext db)
    {
        var userId = GetUserId(principal);
        if (userId is null) return Results.Unauthorized();

        var subscription = await db.Subscriptions.AsNoTracking()
            .Where(x => x.UserId == userId && (x.Status == SubscriptionStatus.Active || x.Status == SubscriptionStatus.Pending))
            .OrderByDescending(x => x.StartedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.Status,
                x.StartedAtUtc,
                x.CurrentPeriodStartUtc,
                x.CurrentPeriodEndUtc,
                x.CancelAtPeriodEnd,
                x.CancelledAtUtc,
                x.Provider,
                Plan = new { x.Plan.Id, x.Plan.Code, x.Plan.Name, x.Plan.PriceAmount, x.Plan.Currency, x.Plan.BillingPeriod }
            })
            .FirstOrDefaultAsync();

        var payments = await db.Payments.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(20)
            .Select(x => new { x.Id, x.Amount, x.Currency, x.Status, x.Provider, x.CreatedAtUtc, x.PaidAtUtc })
            .ToListAsync();

        var invoices = await db.Invoices.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.IssuedAtUtc)
            .Take(20)
            .Select(x => new { x.Id, x.Number, x.Amount, x.Currency, x.Status, x.IssuedAtUtc, x.PaidAtUtc })
            .ToListAsync();

        return Results.Ok(new { subscription, payments, invoices });
    }

    private static async Task<IResult> StartCheckoutAsync(
        CheckoutRequest request,
        ClaimsPrincipal principal,
        KogniaDbContext db)
    {
        var userId = GetUserId(principal);
        if (userId is null) return Results.Unauthorized();

        var plan = await db.SubscriptionPlans.SingleOrDefaultAsync(x => x.Id == request.PlanId && x.IsActive);
        if (plan is null) return Results.NotFound(new { message = "Subscription plan not found." });

        var existing = await db.Subscriptions
            .Where(x => x.UserId == userId && x.Status == SubscriptionStatus.Active)
            .OrderByDescending(x => x.StartedAtUtc)
            .FirstOrDefaultAsync();

        if (existing is not null && existing.PlanId == plan.Id && !existing.CancelAtPeriodEnd)
            return Results.Conflict(new { message = "This plan is already active." });

        var now = DateTimeOffset.UtcNow;
        var periodEnd = plan.BillingPeriod == BillingPeriod.Annual ? now.AddYears(1) : now.AddMonths(1);
        var subscription = new Subscription
        {
            UserId = userId,
            PlanId = plan.Id,
            Status = plan.PriceAmount == 0 ? SubscriptionStatus.Active : SubscriptionStatus.Pending,
            StartedAtUtc = now,
            CurrentPeriodStartUtc = now,
            CurrentPeriodEndUtc = periodEnd,
            Provider = plan.PriceAmount == 0 ? "Internal" : "Sandbox"
        };

        db.Subscriptions.Add(subscription);

        if (plan.PriceAmount == 0)
        {
            if (existing is not null)
            {
                existing.Status = SubscriptionStatus.Cancelled;
                existing.CancelledAtUtc = now;
            }

            await db.SaveChangesAsync();
            return Results.Ok(new
            {
                subscription.Id,
                subscription.Status,
                requiresPayment = false,
                plan = new { plan.Id, plan.Code, plan.Name, plan.PriceAmount, plan.Currency }
            });
        }

        var checkoutToken = $"KOGPAY-{Guid.NewGuid():N}".ToUpperInvariant();
        var payment = new Payment
        {
            Subscription = subscription,
            UserId = userId,
            Amount = plan.PriceAmount,
            Currency = plan.Currency,
            Status = PaymentStatus.Pending,
            Provider = "Sandbox",
            CheckoutToken = checkoutToken
        };
        var invoice = new Invoice
        {
            Subscription = subscription,
            UserId = userId,
            Number = $"KOG-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..25].ToUpperInvariant(),
            Amount = plan.PriceAmount,
            Currency = plan.Currency,
            Status = InvoiceStatus.Open,
            IssuedAtUtc = now
        };

        db.Payments.Add(payment);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            subscription.Id,
            subscription.Status,
            requiresPayment = true,
            checkoutToken,
            provider = "Sandbox",
            amount = plan.PriceAmount,
            plan.Currency,
            plan = new { plan.Id, plan.Code, plan.Name }
        });
    }

    private static async Task<IResult> CompleteSandboxCheckoutAsync(
        string checkoutToken,
        ClaimsPrincipal principal,
        IWebHostEnvironment environment,
        KogniaDbContext db)
    {
        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
            return Results.NotFound();

        var userId = GetUserId(principal);
        if (userId is null) return Results.Unauthorized();

        var payment = await db.Payments
            .Include(x => x.Subscription)
            .SingleOrDefaultAsync(x => x.CheckoutToken == checkoutToken && x.UserId == userId);
        if (payment is null) return Results.NotFound(new { message = "Checkout not found." });
        if (payment.Status == PaymentStatus.Paid)
            return Results.Ok(new { payment.Id, payment.Status, payment.Subscription.Status });

        var now = DateTimeOffset.UtcNow;
        var otherActive = await db.Subscriptions
            .Where(x => x.UserId == userId && x.Status == SubscriptionStatus.Active && x.Id != payment.SubscriptionId)
            .ToListAsync();
        foreach (var item in otherActive)
        {
            item.Status = SubscriptionStatus.Cancelled;
            item.CancelledAtUtc = now;
        }

        payment.Status = PaymentStatus.Paid;
        payment.PaidAtUtc = now;
        payment.ProviderPaymentId = $"sandbox_{Guid.NewGuid():N}";
        payment.Subscription.Status = SubscriptionStatus.Active;
        payment.Subscription.ProviderSubscriptionId = $"sandbox_sub_{Guid.NewGuid():N}";

        var invoice = await db.Invoices.SingleAsync(x => x.SubscriptionId == payment.SubscriptionId);
        invoice.Status = InvoiceStatus.Paid;
        invoice.PaidAtUtc = now;

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            payment.Id,
            payment.Status,
            subscriptionId = payment.SubscriptionId,
            subscriptionStatus = payment.Subscription.Status,
            invoice = new { invoice.Id, invoice.Number, invoice.Status }
        });
    }

    private static async Task<IResult> CancelSubscriptionAsync(ClaimsPrincipal principal, KogniaDbContext db)
    {
        var userId = GetUserId(principal);
        if (userId is null) return Results.Unauthorized();

        var subscription = await db.Subscriptions
            .Where(x => x.UserId == userId && x.Status == SubscriptionStatus.Active)
            .OrderByDescending(x => x.StartedAtUtc)
            .FirstOrDefaultAsync();
        if (subscription is null) return Results.NotFound(new { message = "No active subscription found." });

        subscription.CancelAtPeriodEnd = true;
        await db.SaveChangesAsync();
        return Results.Ok(new { subscription.Id, subscription.CancelAtPeriodEnd, subscription.CurrentPeriodEndUtc });
    }

    private static async Task<IResult> GetStudentDashboardAsync(ClaimsPrincipal principal, KogniaDbContext db)
    {
        var userId = GetUserId(principal);
        if (userId is null) return Results.Unauthorized();

        var courses = await db.Enrollments.AsNoTracking()
            .Where(x => x.StudentUserId == userId)
            .OrderByDescending(x => x.EnrolledAtUtc)
            .Select(x => new
            {
                x.Id,
                x.CourseId,
                x.Course.Title,
                x.Course.Slug,
                x.Status,
                x.EnrolledAtUtc,
                x.CompletedAtUtc,
                TotalLessons = x.Course.Sections.SelectMany(s => s.Lessons).Count(),
                CompletedLessons = x.LessonProgress.Count(p => p.IsCompleted)
            })
            .ToListAsync();

        var courseCards = courses.Select(x => new
        {
            enrollmentId = x.Id,
            x.CourseId,
            x.Title,
            x.Slug,
            x.Status,
            x.EnrolledAtUtc,
            x.CompletedAtUtc,
            x.TotalLessons,
            x.CompletedLessons,
            progressPercent = x.TotalLessons == 0 ? 0 : (int)Math.Round(x.CompletedLessons * 100d / x.TotalLessons)
        }).ToList();

        var certificateCount = await db.Certificates.CountAsync(x => x.StudentUserId == userId);
        var attempts = await db.QuizAttempts.CountAsync(x => x.StudentUserId == userId);
        var passedAttempts = await db.QuizAttempts.CountAsync(x => x.StudentUserId == userId && x.Passed);
        var completedLessons = await db.LessonProgress.CountAsync(x => x.Enrollment.StudentUserId == userId && x.IsCompleted);

        var recentActivity = await db.LessonProgress.AsNoTracking()
            .Where(x => x.Enrollment.StudentUserId == userId)
            .OrderByDescending(x => x.LastAccessedAtUtc)
            .Take(5)
            .Select(x => new
            {
                x.LessonId,
                LessonTitle = x.Lesson.Title,
                CourseTitle = x.Enrollment.Course.Title,
                x.IsCompleted,
                x.LastAccessedAtUtc
            })
            .ToListAsync();

        var subscription = await db.Subscriptions.AsNoTracking()
            .Where(x => x.UserId == userId && x.Status == SubscriptionStatus.Active)
            .OrderByDescending(x => x.StartedAtUtc)
            .Select(x => new { x.Plan.Code, x.Plan.Name, x.CurrentPeriodEndUtc, x.CancelAtPeriodEnd })
            .FirstOrDefaultAsync();

        return Results.Ok(new
        {
            stats = new
            {
                activeCourses = courses.Count(x => x.Status == EnrollmentStatus.Active),
                completedCourses = courses.Count(x => x.Status == EnrollmentStatus.Completed),
                completedLessons,
                certificateCount,
                quizAttempts = attempts,
                passedQuizAttempts = passedAttempts,
                averageProgressPercent = courseCards.Count == 0 ? 0 : (int)Math.Round(courseCards.Average(x => x.progressPercent))
            },
            subscription,
            courses = courseCards,
            recentActivity
        });
    }

    private static async Task<IResult> GetInstructorDashboardAsync(ClaimsPrincipal principal, KogniaDbContext db)
    {
        var userId = GetUserId(principal);
        if (userId is null) return Results.Unauthorized();
        var isAdmin = principal.IsInRole("Administrator");

        var courseQuery = db.Courses.AsNoTracking().AsQueryable();
        if (!isAdmin) courseQuery = courseQuery.Where(x => x.InstructorUserId == userId);

        var courseIds = await courseQuery.Select(x => x.Id).ToListAsync();
        var totalCourses = courseIds.Count;
        var publishedCourses = await courseQuery.CountAsync(x => x.Status == Domain.Catalog.CourseStatus.Published);
        var draftCourses = await courseQuery.CountAsync(x => x.Status == Domain.Catalog.CourseStatus.Draft);
        var totalEnrollments = await db.Enrollments.CountAsync(x => courseIds.Contains(x.CourseId));
        var completedEnrollments = await db.Enrollments.CountAsync(x => courseIds.Contains(x.CourseId) && x.Status == EnrollmentStatus.Completed);
        var certificateCount = await db.Certificates.CountAsync(x => courseIds.Contains(x.CourseId));
        var quizAttempts = await db.QuizAttempts.CountAsync(x => courseIds.Contains(x.Quiz.CourseId));
        var passedQuizAttempts = await db.QuizAttempts.CountAsync(x => courseIds.Contains(x.Quiz.CourseId) && x.Passed);

        var coursePerformance = await courseQuery
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Status,
                enrollments = db.Enrollments.Count(e => e.CourseId == x.Id),
                completions = db.Enrollments.Count(e => e.CourseId == x.Id && e.Status == EnrollmentStatus.Completed),
                certificates = db.Certificates.Count(c => c.CourseId == x.Id),
                quizAttempts = db.QuizAttempts.Count(a => a.Quiz.CourseId == x.Id),
                passedQuizAttempts = db.QuizAttempts.Count(a => a.Quiz.CourseId == x.Id && a.Passed)
            })
            .ToListAsync();

        return Results.Ok(new
        {
            stats = new
            {
                totalCourses,
                publishedCourses,
                draftCourses,
                totalEnrollments,
                completedEnrollments,
                completionRatePercent = totalEnrollments == 0 ? 0 : Math.Round(completedEnrollments * 100m / totalEnrollments, 1),
                certificateCount,
                quizAttempts,
                passedQuizAttempts,
                quizPassRatePercent = quizAttempts == 0 ? 0 : Math.Round(passedQuizAttempts * 100m / quizAttempts, 1)
            },
            courses = coursePerformance
        });
    }

    public sealed record CheckoutRequest(Guid PlanId);
}
