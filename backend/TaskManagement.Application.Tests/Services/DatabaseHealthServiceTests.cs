using FluentAssertions;
using Moq;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;

namespace TaskManagement.Application.Tests.Services;

public sealed class DatabaseHealthServiceTests
{
    [Fact]
    public async Task CheckAsync_ShouldDelegateToRepository()
    {
        var repositoryMock = new Mock<IDatabaseHealthRepository>();
        var service = new DatabaseHealthService(repositoryMock.Object);

        var cancellationToken = CancellationToken.None;

        repositoryMock
            .Setup(x => x.CheckAsync(cancellationToken))
            .Returns(Task.CompletedTask);

        await service.CheckAsync(cancellationToken);

        repositoryMock.Verify(
            x => x.CheckAsync(cancellationToken),
            Times.Once);
    }
}