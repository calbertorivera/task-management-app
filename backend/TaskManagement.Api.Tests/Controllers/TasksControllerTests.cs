using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TaskManagement.Api.Controllers;
using TaskManagement.Application.Contracts.Tasks;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Api.Tests.Controllers;

public sealed class TasksControllerTests
{
    [Fact]
    public async Task GetAll_WhenUserIsAuthenticated_ShouldReturnOk()
    {
        var userId = Guid.NewGuid();
        var serviceMock = new Mock<ITaskService>();

        var tasks = new List<TaskResponse>
        {
            new(
                Guid.NewGuid(),
                "Task 1",
                "Description",
                TaskItemStatus.Pending,
                null,
                userId)
        };

        serviceMock
            .Setup(x => x.GetAllAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);

        var controller = CreateController(serviceMock.Object, userId);

        var result = await controller.GetAll(CancellationToken.None);

        var okResult = result.Result
            .Should()
            .BeOfType<OkObjectResult>()
            .Subject;

        okResult.Value.Should().BeEquivalentTo(tasks);
    }

    [Fact]
    public async Task GetAll_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        var serviceMock = new Mock<ITaskService>();
        var controller = CreateController(serviceMock.Object);

        var result = await controller.GetAll(CancellationToken.None);

        result.Result.Should().BeOfType<UnauthorizedResult>();

        serviceMock.Verify(
            x => x.GetAllAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetById_WhenTaskExists_ShouldReturnOk()
    {
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var serviceMock = new Mock<ITaskService>();

        var task = new TaskResponse(
            taskId,
            "Task",
            "Description",
            TaskItemStatus.Pending,
            null,
            userId);

        serviceMock
            .Setup(x => x.GetByIdAsync(
                taskId,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var controller = CreateController(serviceMock.Object, userId);

        var result = await controller.GetById(
            taskId,
            CancellationToken.None);

        var okResult = result.Result
            .Should()
            .BeOfType<OkObjectResult>()
            .Subject;

        okResult.Value.Should().BeEquivalentTo(task);
    }

    [Fact]
    public async Task GetById_WhenTaskDoesNotExist_ShouldReturnNotFound()
    {
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var serviceMock = new Mock<ITaskService>();

        serviceMock
            .Setup(x => x.GetByIdAsync(
                taskId,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskResponse?)null);

        var controller = CreateController(serviceMock.Object, userId);

        var result = await controller.GetById(
            taskId,
            CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_WhenRequestIsValid_ShouldReturnCreated()
    {
        var userId = Guid.NewGuid();
        var serviceMock = new Mock<ITaskService>();

        var request = new CreateTaskRequest(
            "New task",
            "Description",
            null);

        var response = new TaskResponse(
            Guid.NewGuid(),
            request.Title,
            request.Description,
            TaskItemStatus.Pending,
            request.DueDate,
            userId);

        serviceMock
            .Setup(x => x.CreateAsync(
                request,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var controller = CreateController(serviceMock.Object, userId);

        var result = await controller.Create(
            request,
            CancellationToken.None);

        var createdResult = result.Result
            .Should()
            .BeOfType<CreatedAtActionResult>()
            .Subject;

        createdResult.StatusCode
            .Should()
            .Be(StatusCodes.Status201Created);

        createdResult.ActionName
            .Should()
            .Be(nameof(TasksController.GetById));

        createdResult.Value
            .Should()
            .BeEquivalentTo(response);
    }

    [Fact]
    public async Task Create_WhenServiceThrowsArgumentException_ShouldReturnBadRequest()
    {
        var userId = Guid.NewGuid();
        var serviceMock = new Mock<ITaskService>();

        serviceMock
            .Setup(x => x.CreateAsync(
                It.IsAny<CreateTaskRequest>(),
                userId,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException(
                "The title is required."));

        var controller = CreateController(serviceMock.Object, userId);

        var result = await controller.Create(
            new CreateTaskRequest("", "Description", null),
            CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Update_WhenTaskExists_ShouldReturnOk()
    {
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var serviceMock = new Mock<ITaskService>();

        var request = new UpdateTaskRequest(
            "Updated task",
            "Updated description",
            TaskItemStatus.Completed,
            null);

        var response = new TaskResponse(
            taskId,
            request.Title,
            request.Description,
            request.Status,
            request.DueDate,
            userId);

        serviceMock
            .Setup(x => x.UpdateAsync(
                taskId,
                request,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var controller = CreateController(serviceMock.Object, userId);

        var result = await controller.Update(
            taskId,
            request,
            CancellationToken.None);

        var okResult = result.Result
            .Should()
            .BeOfType<OkObjectResult>()
            .Subject;

        okResult.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task Update_WhenTaskDoesNotExist_ShouldReturnNotFound()
    {
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var serviceMock = new Mock<ITaskService>();

        var request = new UpdateTaskRequest(
            "Updated task",
            "Description",
            TaskItemStatus.InProgress,
            null);

        serviceMock
            .Setup(x => x.UpdateAsync(
                taskId,
                request,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskResponse?)null);

        var controller = CreateController(serviceMock.Object, userId);

        var result = await controller.Update(
            taskId,
            request,
            CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_WhenTaskExists_ShouldReturnNoContent()
    {
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var serviceMock = new Mock<ITaskService>();

        serviceMock
            .Setup(x => x.DeleteAsync(
                taskId,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var controller = CreateController(serviceMock.Object, userId);

        var result = await controller.Delete(
            taskId,
            CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WhenTaskDoesNotExist_ShouldReturnNotFound()
    {
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var serviceMock = new Mock<ITaskService>();

        serviceMock
            .Setup(x => x.DeleteAsync(
                taskId,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var controller = CreateController(serviceMock.Object, userId);

        var result = await controller.Delete(
            taskId,
            CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    private static TasksController CreateController(
        ITaskService taskService,
        Guid? userId = null)
    {
        var identity = userId.HasValue
            ? new ClaimsIdentity(
                new[]
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        userId.Value.ToString())
                },
                "TestAuthentication")
            : new ClaimsIdentity();

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };

        return new TasksController(taskService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };
    }
}