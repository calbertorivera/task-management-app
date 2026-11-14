using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Interfaces;
using TaskManagement.Infrastructure.Data;

namespace TaskManagement.Infrastructure.Repositories;

public sealed class DatabaseHealthRepository(
    TaskManagementDbContext dbContext)
    : IDatabaseHealthRepository
{
    public async Task CheckAsync(
        CancellationToken cancellationToken = default)
    {
        if (!dbContext.Database.IsRelational())
        {
            await dbContext.Database.CanConnectAsync(
                cancellationToken);

            return;
        }

        dbContext.Database.SetCommandTimeout(180);

        var executionStrategy =
            dbContext.Database.CreateExecutionStrategy();

        await executionStrategy.ExecuteAsync(async () =>
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "SELECT 1",
                cancellationToken);
        });
    }
}