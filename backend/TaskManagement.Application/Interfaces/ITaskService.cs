using TaskManagement.Application.Contracts.Tasks;

namespace TaskManagement.Application.Interfaces;

public interface ITaskService
{
    Task<IReadOnlyList<TaskResponse>> GetAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<TaskResponse?> GetByIdAsync(
        Guid taskId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<TaskResponse> CreateAsync(
        CreateTaskRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<TaskResponse?> UpdateAsync(
        Guid taskId,
        UpdateTaskRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid taskId,
        Guid userId,
        CancellationToken cancellationToken = default);
}