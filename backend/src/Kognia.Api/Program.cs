using Kognia.Api.Endpoints;
using Kognia.Application;
using Kognia.Infrastructure;
using Kognia.Infrastructure.Identity;
using Kognia.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing"))
{
    var productionJwt = builder.Configuration["Jwt:Key"];
    var productionConnection = builder.Configuration.GetConnectionString("KogniaConnection");
    if (string.IsNullOrWhiteSpace(productionJwt) || productionJwt.Contains("DEV_ONLY", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("Production requires Jwt:Key from secure configuration.");
    if (string.IsNullOrWhiteSpace(productionConnection) || productionConnection.Contains("Your_password123", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("Production requires KogniaConnection from secure configuration.");
}

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var allowedOrigins = builder.Configuration.GetSection("Security:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("KogniaCors", policy =>
{
    if (allowedOrigins.Length > 0)
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
}));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

var app = builder.Build();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment()) app.MapOpenApi();
else app.UseHsts();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";
    await next();
});

app.UseHttpsRedirection();
app.UseCors("KogniaCors");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<KogniaDbContext>();
    if (app.Environment.IsEnvironment("Testing")) await db.Database.EnsureCreatedAsync();
    else await db.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    await IdentitySeeder.SeedAsync(roleManager);

    if (app.Environment.IsDevelopment())
    {
        var seedEmail = builder.Configuration["DevelopmentSeed:Email"];
        var seedPassword = builder.Configuration["DevelopmentSeed:Password"];
        var seedFirstName = builder.Configuration["DevelopmentSeed:FirstName"] ?? "Kognia";
        var seedLastName = builder.Configuration["DevelopmentSeed:LastName"] ?? "Student";

        if (!string.IsNullOrWhiteSpace(seedEmail) && !string.IsNullOrWhiteSpace(seedPassword))
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            await IdentitySeeder.SeedDevelopmentUserAsync(userManager, seedEmail, seedPassword, seedFirstName, seedLastName);
        }
    }

    await CatalogSeeder.SeedAsync(db);
    await BillingSeeder.SeedAsync(db);
}

app.MapGet("/health", () => Results.Ok(new { service = "Kognia.Api", status = "ok", utc = DateTimeOffset.UtcNow }));
app.MapGet("/health/live", () => Results.Ok(new { status = "live", utc = DateTimeOffset.UtcNow }));
app.MapGet("/health/ready", async (KogniaDbContext db) =>
    await db.Database.CanConnectAsync()
        ? Results.Ok(new { status = "ready", utc = DateTimeOffset.UtcNow })
        : Results.Problem("Database is not ready.", statusCode: StatusCodes.Status503ServiceUnavailable));

app.MapAuthEndpoints();
app.MapCatalogEndpoints();
app.MapBlock2Endpoints();
app.MapBlock3Endpoints();
app.MapBlock4Endpoints();
app.MapFinalBlockEndpoints();

app.Run();

public partial class Program;
