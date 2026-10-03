using Kognia.Api.Endpoints;
using Kognia.Application;
using Kognia.Infrastructure;
using Kognia.Infrastructure.Identity;
using Kognia.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    await IdentitySeeder.SeedAsync(roleManager);

    var db = scope.ServiceProvider.GetRequiredService<KogniaDbContext>();
    await db.Database.EnsureCreatedAsync();
    await CatalogSeeder.SeedAsync(db);
}

app.MapGet("/health", () => Results.Ok(new
{
    service = "Kognia.Api",
    status = "ok",
    utc = DateTimeOffset.UtcNow
}));

app.MapAuthEndpoints();
app.MapCatalogEndpoints();

app.Run();
