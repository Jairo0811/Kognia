using System.Net;
using System.Net.Http.Json;
using Kognia.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kognia.Api.IntegrationTests;

public sealed class KogniaApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<KogniaDbContext>));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<KogniaDbContext>(options =>
                options.UseInMemoryDatabase($"KogniaTests-{Guid.NewGuid()}"));
        });
    }
}

public sealed class Block1ApiTests(KogniaApiFactory factory) : IClassFixture<KogniaApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Health_returns_ok()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Catalog_categories_are_public_and_seeded()
    {
        var response = await _client.GetAsync("/api/catalog/categories");
        response.EnsureSuccessStatusCode();

        var categories = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        Assert.NotNull(categories);
        Assert.NotEmpty(categories);
    }

    [Fact]
    public async Task Instructor_courses_requires_authentication()
    {
        var response = await _client.GetAsync("/api/instructor/courses");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Registration_creates_student_account()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Integration",
            lastName = "Student",
            email = $"student-{Guid.NewGuid():N}@kognia.test",
            password = "Kognia!2026"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private sealed record CategoryResponse(Guid Id, string Name, string Slug, string? Description);
}
