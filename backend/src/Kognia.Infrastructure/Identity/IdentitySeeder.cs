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

    public static async Task SeedDevelopmentUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string firstName,
        string lastName)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(normalizedEmail);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = normalizedEmail,
                Email = normalizedEmail,
                FirstName = firstName.Trim(),
                LastName = lastName.Trim(),
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
                throw new InvalidOperationException($"Unable to seed development user: {string.Join("; ", createResult.Errors.Select(error => error.Description))}");
        }
        else
        {
            var changed = false;
            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                changed = true;
            }

            if (user.FirstName != firstName.Trim() || user.LastName != lastName.Trim())
            {
                user.FirstName = firstName.Trim();
                user.LastName = lastName.Trim();
                changed = true;
            }

            if (changed)
            {
                var updateResult = await userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                    throw new InvalidOperationException($"Unable to update development user: {string.Join("; ", updateResult.Errors.Select(error => error.Description))}");
            }

            if (!await userManager.CheckPasswordAsync(user, password))
            {
                var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await userManager.ResetPasswordAsync(user, resetToken, password);
                if (!resetResult.Succeeded)
                    throw new InvalidOperationException($"Unable to reset development user password: {string.Join("; ", resetResult.Errors.Select(error => error.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, "Student"))
        {
            var roleResult = await userManager.AddToRoleAsync(user, "Student");
            if (!roleResult.Succeeded)
                throw new InvalidOperationException($"Unable to assign Student role to development user: {string.Join("; ", roleResult.Errors.Select(error => error.Description))}");
        }
    }
}
