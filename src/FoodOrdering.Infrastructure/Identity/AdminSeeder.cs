using FoodOrdering.Application.Auth;
using FoodOrdering.Infrastructure.Database;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FoodOrdering.Infrastructure.Identity;

public static class AdminSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var configuration = provider.GetRequiredService<IConfiguration>();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("AdminSeeder");
        var db = provider.GetRequiredService<AppDbContext>();

        if (!await db.Database.CanConnectAsync())
        {
            logger.LogWarning("Skipping admin seed because database is not reachable yet.");
            return;
        }

        var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            logger.LogWarning("Skipping admin seed because pending EF Core migrations have not been applied yet.");
            return;
        }

        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        await IdentityUserDirectory.EnsureRoleExistsAsync(roleManager, AppRoles.Admin);
        await IdentityUserDirectory.EnsureRoleExistsAsync(roleManager, AppRoles.Customer);

        var email = configuration["SeedAdmin:Email"]?.Trim();
        var fullName = configuration["SeedAdmin:FullName"]?.Trim();
        var password = configuration["SeedAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var userManager = provider.GetRequiredService<UserManager<AppUser>>();
        var existingAdmin = await userManager.FindByEmailAsync(email);
        if (existingAdmin is not null)
        {
            if (!await userManager.IsInRoleAsync(existingAdmin, AppRoles.Admin))
            {
                await userManager.AddToRoleAsync(existingAdmin, AppRoles.Admin);
            }
            return;
        }

        var admin = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FullName = string.IsNullOrWhiteSpace(fullName) ? "Platform Admin" : fullName
        };

        var result = await userManager.CreateAsync(admin, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogWarning("Failed to seed admin user '{Email}': {Errors}", email, errors);
            return;
        }

        await userManager.AddToRoleAsync(admin, AppRoles.Admin);
        logger.LogInformation("Seeded platform Admin user '{Email}'.", email);
    }
}
