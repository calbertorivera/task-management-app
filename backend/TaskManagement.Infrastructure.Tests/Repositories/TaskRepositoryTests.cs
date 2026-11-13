using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;
using TaskManagement.Infrastructure.Data;
using TaskManagement.Infrastructure.Repositories;

namespace TaskManagement.Infrastructure.Tests.Repositories;

public sealed class TaskRepositoryTests
{
    [Fact]
    public async Task AddAsync_AndGetByIdAsync_ShouldPersistTask()
    {
        await using var context = CreateContext();

        var userId = Guid.NewGuid();
        var task = new TaskItem(
            "Test task",
            "Description",
            DateTime.UtcNow.AddDays(1),
            userId);

        var repository = new TaskRepository(context);

        await repository.AddAsync(task);
        await repository.SaveChangesAsync();

        var result = await repository.GetByIdAsync(task.Id, userId);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Test task");
        result.Description.Should().Be("Description");
        result.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnOnlyUserTasksOrderedByDueDate()
    {
        await using var context = CreateContext();

        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var baseDate = DateTime.UtcNow;

        var laterTask = new TaskItem(
            "Later task",
            "Description",
            baseDate.AddDays(2),
            userId);

        var earlierTask = new TaskItem(
            "Earlier task",
            "Description",
            baseDate.AddDays(1),
            userId);

        var otherUserTask = new TaskItem(
            "Other user task",
            "Description",
            baseDate,
            otherUserId);

        context.Tasks.AddRange(
            laterTask,
            earlierTask,
            otherUserTask);

        await context.SaveChangesAsync();

        var repository = new TaskRepository(context);

        var result = await repository.GetByUserIdAsync(userId);

        result.Should().HaveCount(2);
        result.Select(task => task.Id)
            .Should()
            .ContainInOrder(earlierTask.Id, laterTask.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenTaskBelongsToAnotherUser_ShouldReturnNull()
    {
        await using var context = CreateContext();

        var ownerId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();

        var task = new TaskItem(
            "Private task",
            "Description",
            null,
            ownerId);

        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        var repository = new TaskRepository(context);

        var result = await repository.GetByIdAsync(
            task.Id,
            anotherUserId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Update_ShouldPersistChanges()
    {
        await using var context = CreateContext();

        var userId = Guid.NewGuid();

        var task = new TaskItem(
            "Original title",
            "Original description",
            null,
            userId);

        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        var repository = new TaskRepository(context);

        task.Update(
            "Updated title",
            "Updated description",
            TaskItemStatus.Completed,
            DateTime.UtcNow.AddDays(5));

        repository.Update(task);
        await repository.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result = await repository.GetByIdAsync(task.Id, userId);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Updated title");
        result.Description.Should().Be("Updated description");
        result.Status.Should().Be(TaskItemStatus.Completed);
    }

    [Fact]
    public async Task Delete_ShouldRemoveTask()
    {
        await using var context = CreateContext();

        var userId = Guid.NewGuid();

        var task = new TaskItem(
            "Task to delete",
            "Description",
            null,
            userId);

        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        var repository = new TaskRepository(context);

        repository.Delete(task);
        await repository.SaveChangesAsync();

        var result = await repository.GetByIdAsync(task.Id, userId);

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