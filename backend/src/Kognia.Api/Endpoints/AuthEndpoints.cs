using System.Security.Claims;
using Kognia.Api.Contracts.Auth;
using Kognia.Infrastructure.Identity;
using Kognia.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Kognia.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Identity");

        group.MapPost("/register", RegisterAsync);
        group.MapPost("/login", LoginAsync);
        group.MapPost("/refresh", RefreshAsync);
        group.MapPost("/confirm-email", ConfirmEmailAsync);
        group.MapPost("/forgot-password", ForgotPasswordAsync);
        group.MapPost("/reset-password", ResetPasswordAsync);
        group.MapGet("/me", MeAsync).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        UserManager<ApplicationUser> userManager,
        ILogger<AuthEndpointMarker> logger)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await userManager.FindByEmailAsync(email) is not null)
            return Results.Conflict(new { message = "An account with this email already exists." });

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim()
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return Results.ValidationProblem(result.Errors.ToDictionary(x => x.Code, x => new[] { x.Description }));

        await userManager.AddToRoleAsync(user, "Student");
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        logger.LogInformation("Kognia email confirmation token for user {UserId}: {Token}", user.Id, token);

        return Results.Created($"/api/auth/users/{user.Id}", new
        {
            user.Id,
            user.Email,
            message = "Account created. Email confirmation is required before sign in."
        });
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        UserManager<ApplicationUser> userManager,
        JwtTokenService tokenService,
        KogniaDbContext db,
        IOptions<JwtOptions> jwtOptions)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim().ToLowerInvariant());
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
            return Results.Unauthorized();

        if (!await userManager.IsEmailConfirmedAsync(user))
            return Results.Json(new { message = "Email confirmation is required." }, statusCode: StatusCodes.Status403Forbidden);

        return Results.Ok(await IssueTokensAsync(user, userManager, tokenService, db, jwtOptions.Value));
    }

    private static async Task<IResult> RefreshAsync(
        RefreshRequest request,
        UserManager<ApplicationUser> userManager,
        JwtTokenService tokenService,
        KogniaDbContext db,
        IOptions<JwtOptions> jwtOptions)
    {
        var hash = JwtTokenService.HashRefreshToken(request.RefreshToken);
        var stored = await db.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == hash);
        if (stored is null || !stored.IsActive)
            return Results.Unauthorized();

        var user = await userManager.FindByIdAsync(stored.UserId);
        if (user is null)
            return Results.Unauthorized();

        stored.RevokedAtUtc = DateTimeOffset.UtcNow;
        return Results.Ok(await IssueTokensAsync(user, userManager, tokenService, db, jwtOptions.Value));
    }

    private static async Task<IResult> ConfirmEmailAsync(ConfirmEmailRequest request, UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user is null) return Results.BadRequest(new { message = "Invalid confirmation request." });

        var result = await userManager.ConfirmEmailAsync(user, request.Token);
        return result.Succeeded
            ? Results.Ok(new { message = "Email confirmed." })
            : Results.ValidationProblem(result.Errors.ToDictionary(x => x.Code, x => new[] { x.Description }));
    }

    private static async Task<IResult> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        UserManager<ApplicationUser> userManager,
        ILogger<AuthEndpointMarker> logger)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim().ToLowerInvariant());
        if (user is not null && await userManager.IsEmailConfirmedAsync(user))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            logger.LogInformation("Kognia password reset token for user {UserId}: {Token}", user.Id, token);
        }

        return Results.Accepted(value: new { message = "If the account exists, password reset instructions will be sent." });
    }

    private static async Task<IResult> ResetPasswordAsync(ResetPasswordRequest request, UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim().ToLowerInvariant());
        if (user is null) return Results.BadRequest(new { message = "Invalid password reset request." });

        var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        return result.Succeeded
            ? Results.Ok(new { message = "Password updated." })
            : Results.ValidationProblem(result.Errors.ToDictionary(x => x.Code, x => new[] { x.Description }));
    }

    private static async Task<IResult> MeAsync(ClaimsPrincipal principal, UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null) return Results.Unauthorized();
        var roles = await userManager.GetRolesAsync(user);

        return Results.Ok(new
        {
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            Roles = roles
        });
    }

    private static async Task<AuthResponse> IssueTokensAsync(
        ApplicationUser user,
        UserManager<ApplicationUser> userManager,
        JwtTokenService tokenService,
        KogniaDbContext db,
        JwtOptions options)
    {
        var (accessToken, expiresAt) = await tokenService.CreateAccessTokenAsync(user);
        var refreshToken = tokenService.CreateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = JwtTokenService.HashRefreshToken(refreshToken),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(options.RefreshTokenDays)
        });
        await db.SaveChangesAsync();

        var roles = await userManager.GetRolesAsync(user);
        return new AuthResponse(accessToken, refreshToken, expiresAt, user.Id, user.Email ?? string.Empty, roles.ToArray());
    }

    public sealed class AuthEndpointMarker;
}
