using Microsoft.AspNetCore.Identity;
using TabulaWMS.Models;

namespace TabulaWMS.Data;

public static class DbInitializer
{
    public static async Task SeedSuperAdminAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // 1. Ensure the SuperAdmin role exists in the public schema
        if (!await roleManager.RoleExistsAsync("SuperAdmin"))
        {
            await roleManager.CreateAsync(new IdentityRole("SuperAdmin"));
        }

        // 2. Read credentials from appsettings.json
        var adminEmail = configuration["SuperAdmin:Email"];
        var adminPassword = configuration["SuperAdmin:Password"];

        // 3. Check if the SuperAdmin already exists
        var existingAdmin = await userManager.FindByEmailAsync(adminEmail!);
        if (existingAdmin == null)
        {
            var superAdmin = new User
            {
                UserName = adminEmail,
                Email = adminEmail,
                Name = "System SuperAdmin",
                Role = "SuperAdmin",
                CreatedBy = "System",
                CreatedAt = DateTime.UtcNow,
                TenantId = "public"
            };

            var result = await userManager.CreateAsync(superAdmin, adminPassword!);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
                Console.WriteLine("[Seeder] SuperAdmin created and assigned role successfully!");
            }
            else
            {
                Console.WriteLine("[Seeder FAILED]:");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($" - {error.Code}: {error.Description}");
                }
            }
        }
        else
        {
            Console.WriteLine("[Seeder] SuperAdmin user already exists in database.");
        }
    }
}