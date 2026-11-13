using FluentAssertions;
using Moq;
using TaskManagement.Application.Contracts.Tasks;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Tests.Services;

public sealed class TaskServiceTests
{
    private readonly Mock<ITaskRepository> _repositoryMock = new();
    private readonly TaskService _service;

    public TaskServiceTests()
    {
        _service = new TaskService(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnUserTasks()
    {
        var userId = Guid.NewGuid();

        var tasks = new List<TaskItem>
        {
            new("Task 1", "Description 1", DateTime.UtcNow.AddDays(1), userId),
            new("Task 2", "Description 2", DateTime.UtcNow.AddDays(2), userId)
        };

        _repositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);

        var result = await _service.GetAllAsync(userId);

        result.Should().HaveCount(2);
        result.Select(x => x.Title)
            .Should()
            .BeEquivalentTo("Task 1", "Task 2");
    }

    [Fact]
    public async Task GetByIdAsync_WhenTaskExists_ShouldReturnTask()
    {
        var userId = Guid.NewGuid();
        var task = new TaskItem(
            "Task",
            "Description",
            DateTime.UtcNow.AddDays(1),
            userId);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(
                task.Id,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var result = await _service.GetByIdAsync(task.Id, userId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(task.Id);
        result.Title.Should().Be("Task");
        result.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task GetByIdAsync_WhenTaskDoesNotExist_ShouldReturnNull()
    {
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(
                taskId,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem?)null);

        var result = await _service.GetByIdAsync(taskId, userId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ShouldTrimValuesAndSaveTask()
    {
        var userId = Guid.NewGuid();
        var dueDate = DateTime.UtcNow.AddDays(3);

        var request = new CreateTaskRequest(
            "  New task  ",
            "  Description  ",
            dueDate);

        _repositoryMock
            .Setup(x => x.AddAsync(
                It.IsAny<TaskItem>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.CreateAsync(request, userId);

        result.Title.Should().Be("New task");
        result.Description.Should().Be("Description");
        result.DueDate.Should().Be(dueDate);
        result.UserId.Should().Be(userId);
        result.Status.Should().Be(TaskItemStatus.Pending);

        _repositoryMock.Verify(x => x.AddAsync(
            It.Is<TaskItem>(task =>
                task.Title == "New task" &&
                task.Description == "Description" &&
                task.UserId == userId),
            It.IsAny<CancellationToken>()), Times.Once);

        _repositoryMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenTitleIsEmpty_ShouldThrowException()
    {
        var request = new CreateTaskRequest(
            "   ",
            "Description",
            null);

        Func<Task> action = () =>
            _service.CreateAsync(request, Guid.NewGuid());

        await action.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*The title is required.*");

        _repositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<TaskItem>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateAndSaveTask()
    {
        var userId = Guid.NewGuid();
        var task = new TaskItem(
            "Old title",
            "Old description",
            null,
            userId);

        var request = new UpdateTaskRequest(
            "  Updated title  ",
            "  Updated description  ",
            TaskItemStatus.Completed,
            DateTime.UtcNow.AddDays(5));

        _repositoryMock
            .Setup(x => x.GetByIdAsync(
                task.Id,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _repositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync(
            task.Id,
            request,
            userId);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Updated title");
        result.Description.Should().Be("Updated description");
        result.Status.Should().Be(TaskItemStatus.Completed);
        result.DueDate.Should().Be(request.DueDate);

        _repositoryMock.Verify(
            x => x.Update(task),
            Times.Once);

        _repositoryMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenTaskDoesNotExist_ShouldReturnNull()
    {
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(
                taskId,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem?)null);

        var request = new UpdateTaskRequest(
            "Updated",
            "Description",
            TaskItemStatus.InProgress,
            null);

        var result = await _service.UpdateAsync(
            taskId,
            request,
            userId);

        result.Should().BeNull();

        _repositoryMock.Verify(
            x => x.Update(It.IsAny<TaskItem>()),
            Times.Never);

        _repositoryMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenTaskExists_ShouldDeleteAndSave()
    {
        var userId = Guid.NewGuid();
        var task = new TaskItem(
            "Task",
            "Description",
            null,
            userId);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(
                task.Id,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _repositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync(task.Id, userId);

        result.Should().BeTrue();

        _repositoryMock.Verify(
            x => x.Delete(task),
            Times.Once);

        _repositoryMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenTaskDoesNotExist_ShouldReturnFalse()
    {
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(
                taskId,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem?)null);

        var result = await _service.DeleteAsync(taskId, userId);

        result.Should().BeFalse();

        _repositoryMock.Verify(
            x => x.Delete(It.IsAny<TaskItem>()),
            Times.Never);

        _repositoryMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }
}