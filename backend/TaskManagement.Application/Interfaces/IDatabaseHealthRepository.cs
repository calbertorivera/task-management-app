namespace TaskManagement.Application.Interfaces;

public interface IDatabaseHealthRepository
{
    Task CheckAsync(
        CancellationToken cancellationToken = default);
}