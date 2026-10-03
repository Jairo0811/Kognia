using Microsoft.AspNetCore.Identity;

namespace Kognia.Infrastructure.Identity;

public static class IdentitySeeder
{
    private static readonly string[] Roles = ["Student", "Instructor", "Administrator"];

    public static async Task SeedAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}
