using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Infrastructure.Data;
using TaskManagement.Infrastructure.Repositories;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Tests.Repositories;

public sealed class UserRepositoryTests
{
    [Fact]
    public async Task AddAsync_AndGetByEmailAsync_ShouldPersistUser()
    {
        await using var context = CreateContext();

        var user = new User(
            "user@example.com",
            "hashed-password");

        var repository = new UserRepository(context);

        await repository.AddAsync(user);
        await repository.SaveChangesAsync();

        var result = await repository.GetByEmailAsync("user@example.com");

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Email.Should().Be("user@example.com");
        result.PasswordHash.Should().Be("hashed-password");
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserDoesNotExist_ShouldReturnNull()
    {
        await using var context = CreateContext();

        var repository = new UserRepository(context);

        var result = await repository.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    private static TaskManagementDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TaskManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TaskManagementDbContext(options);
    }
}