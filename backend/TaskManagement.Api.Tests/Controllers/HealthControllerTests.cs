using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TaskManagement.Api.Controllers;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Api.Tests.Controllers;

public sealed class HealthControllerTests
{
    [Fact]
    public async Task CheckDatabase_WhenDatabaseIsAvailable_ShouldReturnOk()
    {
        var serviceMock = new Mock<IDatabaseHealthService>();

        serviceMock
            .Setup(x => x.CheckAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = new HealthController(
            serviceMock.Object,
            NullLogger<HealthController>.Instance);

        var result = await controller.CheckDatabase(
            CancellationToken.None);

        var okResult = result
            .Should()
            .BeOfType<OkObjectResult>()
            .Subject;

        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckDatabase_WhenDatabaseFails_ShouldReturnServiceUnavailable()
    {
        var serviceMock = new Mock<IDatabaseHealthService>();

        serviceMock
            .Setup(x => x.CheckAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException());

        var controller = new HealthController(
            serviceMock.Object,
            NullLogger<HealthController>.Instance);

        var result = await controller.CheckDatabase(
            CancellationToken.None);

        var objectResult = result
            .Should()
            .BeOfType<ObjectResult>()
            .Subject;

        objectResult.StatusCode.Should().Be(503);
    }

    [Fact]
    public async Task CheckDatabase_WhenOperationTimesOut_ShouldReturnGatewayTimeout()
    {
        var serviceMock = new Mock<IDatabaseHealthService>();
        using var cancellationTokenSource = new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        serviceMock
            .Setup(x => x.CheckAsync(
                cancellationTokenSource.Token))
            .ThrowsAsync(
                new OperationCanceledException(
                    cancellationTokenSource.Token));

        var controller = new HealthController(
            serviceMock.Object,
            NullLogger<HealthController>.Instance);

        var result = await controller.CheckDatabase(
            cancellationTokenSource.Token);

        var objectResult = result
            .Should()
            .BeOfType<ObjectResult>()
            .Subject;

        objectResult.StatusCode.Should().Be(504);
    }
}