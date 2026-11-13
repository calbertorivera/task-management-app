using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Infrastructure.Data;
using TaskManagement.Infrastructure.Repositories;

namespace TaskManagement.Infrastructure.Tests.Repositories;

public sealed class DatabaseHealthRepositoryTests
{
    [Fact]
    public async Task CheckAsync_ShouldExecuteSuccessfully()
    {
        await using var connection = new SqliteConnection(
            "Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<TaskManagementDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context =
            new TaskManagementDbContext(options);

        var repository = new DatabaseHealthRepository(context);

        Func<Task> action = () =>
            repository.CheckAsync();

        await action.Should().NotThrowAsync();
    }
}