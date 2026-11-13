using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;
namespace TaskManagement.Infrastructure.Data;
using Microsoft.Extensions.Configuration;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        TaskManagementDbContext context,
        IPasswordHasher<User> passwordHasher,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var seedEnabled = configuration.GetValue<bool>("Seed:Enabled");

        if (!seedEnabled)
        {
            return;
        }

        var demoEmail = configuration["Seed:DemoEmail"]
            ?? throw new InvalidOperationException(
                "Seed demo email is not configured.");

        var demoPassword = configuration["Seed:DemoPassword"]
            ?? throw new InvalidOperationException(
                "Seed demo password is not configured.");

        var user = await context.Users
            .SingleOrDefaultAsync(
                existingUser => existingUser.Email == demoEmail,
                cancellationToken);

        if (user is null)
        {
            user = new User(demoEmail, string.Empty);

            var passwordHash = passwordHasher.HashPassword(
                user,
                demoPassword);

            user.SetPasswordHash(passwordHash);
            user.MarkEmailAsVerified();

            await context.Users.AddAsync(user, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        if (!user.IsEmailVerified)
        {
            user.MarkEmailAsVerified();
            await context.SaveChangesAsync(cancellationToken);
        }

        var hasTasks = await context.Tasks
            .AnyAsync(task => task.UserId == user.Id, cancellationToken);

        if (hasTasks)
        {
            return;
        }

        var tasks = new[]
        {
            new TaskItem(
                "Review project requirements",
                "Review the technical interview requirements.",
                DateTime.UtcNow.AddDays(2),
                user.Id),

            new TaskItem(
                "Prepare project presentation",
                "Prepare the architecture and implementation presentation.",
                DateTime.UtcNow.AddDays(5),
                user.Id)
        };

        await context.Tasks.AddRangeAsync(tasks, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}