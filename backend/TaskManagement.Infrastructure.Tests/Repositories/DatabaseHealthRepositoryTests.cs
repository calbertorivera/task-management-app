using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Infrastructure.Data;
using TaskManagement.Infrastructure.Repositories;

namespace TaskManagement.Infrastructure.Tests.Repositories;

public sealed class DatabaseHealthRepositoryTests
{
    [Fact]
    public async Task CheckAsync_ShouldExecuteSuccessfully()
    {
        var options =
            new DbContextOptionsBuilder<TaskManagementDbContext>()
                .UseInMemoryDatabase(
                    $"health-check-{Guid.NewGuid()}")
                .Options;

        await using var context =
            new TaskManagementDbContext(options);

        var repository =
            new DatabaseHealthRepository(context);

        Func<Task> action = () =>
            repository.CheckAsync();

        await action.Should().NotThrowAsync();
    }
}