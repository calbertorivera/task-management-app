using TaskManagement.Application.Interfaces;

namespace TaskManagement.Application.Services;

public sealed class DatabaseHealthService(
    IDatabaseHealthRepository databaseHealthRepository)
    : IDatabaseHealthService
{
    public Task CheckAsync(
        CancellationToken cancellationToken = default)
    {
        return databaseHealthRepository.CheckAsync(
            cancellationToken);
    }
}