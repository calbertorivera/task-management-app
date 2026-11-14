using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Domain.Entities;
using TaskManagement.Infrastructure.Data;

namespace TaskManagement.Api.Extensions;

public static class DatabaseInitializationExtensions
{
    public static async Task InitializeTaskManagementDatabaseAsync(
        this WebApplication app,
        CancellationToken cancellationToken = default)
    {
        await using var scope = app.Services.CreateAsyncScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<TaskManagementDbContext>();

        var passwordHasher = scope.ServiceProvider
            .GetRequiredService<IPasswordHasher<User>>();

        await dbContext.Database.MigrateAsync(cancellationToken);

        await DatabaseSeeder.SeedAsync(
            dbContext,
            passwordHasher,
            app.Configuration);
    }
}