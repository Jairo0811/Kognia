namespace Kognia.Api.Contracts.Auth;

public sealed record RegisterRequest(string FirstName, string LastName, string Email, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);
public sealed record ConfirmEmailRequest(string UserId, string Token);

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string UserId,
    string Email,
    IReadOnlyCollection<string> Roles);
