namespace TaskManagement.Application.Interfaces;

public interface IDatabaseHealthService
{
    Task CheckAsync(
        CancellationToken cancellationToken = default);
}