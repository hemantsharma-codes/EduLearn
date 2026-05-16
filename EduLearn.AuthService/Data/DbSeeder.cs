using EduLearn.AuthService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


namespace EduLearn.AuthService.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAdminAsync(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            var context = serviceProvider.GetRequiredService<UserDbContext>();
            var passwordHasher = serviceProvider.GetRequiredService<IPasswordHasher<User>>();

            // Apply any pending migrations
            await context.Database.MigrateAsync();

            var adminEmail = configuration["SuperAdmin:Email"];
            var adminPassword = configuration["SuperAdmin:Password"];
            var adminFullName = configuration["SuperAdmin:FullName"];

            if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
            {
                return; // Missing configuration, do not seed
            }

            // Seed Admin only if NO admin exists in the system
            if (!await context.Users.AnyAsync(u => u.Role == "ADMIN"))
            {
                var adminUser = new User
                {
                    FullName = adminFullName ?? "Super Admin",
                    Email = adminEmail,
                    Role = "ADMIN",
                    PasswordHash = "", 
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, adminPassword);

                context.Users.Add(adminUser);
                await context.SaveChangesAsync();
                
                // Optional: Log that seed happened
                Console.WriteLine("--> SuperAdmin seeded successfully.");
            }
        }
    }
}
