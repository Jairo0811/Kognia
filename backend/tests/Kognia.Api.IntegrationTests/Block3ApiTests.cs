using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Kognia.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kognia.Api.IntegrationTests;

public sealed class Block3ApiTests(KogniaApiFactory factory) : IClassFixture<KogniaApiFactory>
{
    [Fact]
    public async Task Billing_plans_are_public_and_seeded()
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/billing/plans");
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetArrayLength() >= 3);
        Assert.Contains(json.RootElement.EnumerateArray(), x => x.GetProperty("code").GetString() == "PREMIUM_MONTHLY");
    }

    [Fact]
    public async Task Paid_checkout_can_be_completed_in_testing_and_activates_subscription()
    {
        var token = await CreateUserTokenAsync("Student");
        using var client = CreateAuthorizedClient(token);

        var plans = await client.GetAsync("/api/billing/plans");
        plans.EnsureSuccessStatusCode();
        using var plansJson = JsonDocument.Parse(await plans.Content.ReadAsStringAsync());
        var premiumPlanId = plansJson.RootElement.EnumerateArray()
            .Single(x => x.GetProperty("code").GetString() == "PREMIUM_MONTHLY")
            .GetProperty("id").GetGuid();

        var checkout = await client.PostAsJsonAsync("/api/billing/checkout", new { planId = premiumPlanId });
        checkout.EnsureSuccessStatusCode();
        using var checkoutJson = JsonDocument.Parse(await checkout.Content.ReadAsStringAsync());
        Assert.True(checkoutJson.RootElement.GetProperty("requiresPayment").GetBoolean());
        var checkoutToken = checkoutJson.RootElement.GetProperty("checkoutToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(checkoutToken));

        var complete = await client.PostAsync($"/api/billing/sandbox/checkout/{checkoutToken}/complete", null);
        complete.EnsureSuccessStatusCode();

        var billing = await client.GetAsync("/api/billing/me");
        billing.EnsureSuccessStatusCode();
        using var billingJson = JsonDocument.Parse(await billing.Content.ReadAsStringAsync());
        Assert.Equal(1, billingJson.RootElement.GetProperty("subscription").GetProperty("status").GetInt32());
        Assert.Contains(billingJson.RootElement.GetProperty("payments").EnumerateArray(), x => x.GetProperty("status").GetInt32() == 1);
        Assert.Contains(billingJson.RootElement.GetProperty("invoices").EnumerateArray(), x => x.GetProperty("status").GetInt32() == 1);
    }

    [Fact]
    public async Task Dashboard_routes_apply_authentication_and_role_rules()
    {
        using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/dashboard/student")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/dashboard/instructor")).StatusCode);

        var studentToken = await CreateUserTokenAsync("Student");
        using var student = CreateAuthorizedClient(studentToken);
        var studentDashboard = await student.GetAsync("/api/dashboard/student");
        studentDashboard.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Forbidden, (await student.GetAsync("/api/dashboard/instructor")).StatusCode);

        var instructorToken = await CreateUserTokenAsync("Instructor");
        using var instructor = CreateAuthorizedClient(instructorToken);
        var instructorDashboard = await instructor.GetAsync("/api/dashboard/instructor");
        instructorDashboard.EnsureSuccessStatusCode();
    }

    private HttpClient CreateAuthorizedClient(string token)
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
        var email = $"block3-{role.ToLowerInvariant()}-{Guid.NewGuid():N}@kognia.test";
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = role,
            LastName = "Block3"
        };

        var created = await userManager.CreateAsync(user, "Kognia!2026");
        Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(x => x.Description)));
        var roleResult = await userManager.AddToRoleAsync(user, role);
        Assert.True(roleResult.Succeeded, string.Join("; ", roleResult.Errors.Select(x => x.Description)));

        var (token, _) = await tokenService.CreateAccessTokenAsync(user);
        return token;
    }
}
