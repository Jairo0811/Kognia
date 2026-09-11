namespace Kognia.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "Kognia";
    public string Audience { get; init; } = "Kognia.Web";
    public string Key { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 30;
    public int RefreshTokenDays { get; init; } = 14;
}
