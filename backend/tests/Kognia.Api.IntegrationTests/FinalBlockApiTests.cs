using System.Net;
using System.Net.Http.Headers;
using Kognia.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kognia.Api.IntegrationTests;

public sealed class FinalBlockApiTests(KogniaApiFactory factory) : IClassFixture<KogniaApiFactory>
{
    [Fact]
    public async Task Health_and_security_headers_are_available()
    {
        using var client = factory.CreateClient();
        var live = await client.GetAsync("/health/live");
        live.EnsureSuccessStatusCode();
        Assert.True(live.Headers.Contains("X-Content-Type-Options"));
        Assert.True(live.Headers.Contains("X-Frame-Options"));
        var ready = await client.GetAsync("/health/ready");
        ready.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Analytics_requires_authentication_and_enforces_admin_role()
    {
        using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/analytics/student")).StatusCode);

        var studentToken = await CreateUserTokenAsync("Student");
        using var student = Authorized(studentToken);
        (await student.GetAsync("/api/analytics/student")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Forbidden, (await student.GetAsync("/api/analytics/admin")).StatusCode);

        var adminToken = await CreateUserTokenAsync("Administrator");
        using var admin = Authorized(adminToken);
        (await admin.GetAsync("/api/analytics/admin")).EnsureSuccessStatusCode();
    }

    private HttpClient Authorized(string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<string> CreateUserTokenAsync(string role)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<JwtTokenService>();
        var email = $"final-{role.ToLowerInvariant()}-{Guid.NewGuid():N}@kognia.test";
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, FirstName = "Final", LastName = role };
        var created = await userManager.CreateAsync(user, "Kognia!2026");
        Assert.True(created.Succeeded);
        Assert.True((await userManager.AddToRoleAsync(user, role)).Succeeded);
        var (token, _) = await tokenService.CreateAccessTokenAsync(user);
        return token;
    }
}
