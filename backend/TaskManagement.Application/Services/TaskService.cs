using TaskManagement.Application.Contracts.Tasks;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Services;

public sealed class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;

    public TaskService(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public async Task<IReadOnlyList<TaskResponse>> GetAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var tasks = await _taskRepository.GetByUserIdAsync(
            userId,
            cancellationToken);

        return tasks.Select(MapToResponse).ToList();
    }

    public async Task<TaskResponse?> GetByIdAsync(
        Guid taskId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(
            taskId,
            userId,
            cancellationToken);

        return task is null ? null : MapToResponse(task);
    }

    public async Task<TaskResponse> CreateAsync(
        CreateTaskRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("The title is required.");
        }

        var task = new TaskItem(
            request.Title.Trim(),
            request.Description?.Trim() ?? string.Empty,
            request.DueDate,
            userId);

        await _taskRepository.AddAsync(task, cancellationToken);
        await _taskRepository.SaveChangesAsync(cancellationToken);

        return MapToResponse(task);
    }

    public async Task<TaskResponse?> UpdateAsync(
        Guid taskId,
        UpdateTaskRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(
            taskId,
            userId,
            cancellationToken);

        if (task is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("The title is required.");
        }

        task.Update(
            request.Title.Trim(),
            request.Description?.Trim() ?? string.Empty,
            request.Status,
            request.DueDate);

        _taskRepository.Update(task);
        await _taskRepository.SaveChangesAsync(cancellationToken);

        return MapToResponse(task);
    }

    public async Task<bool> DeleteAsync(
        Guid taskId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(
            taskId,
            userId,
            cancellationToken);

        if (task is null)
        {
            return false;
        }

        _taskRepository.Delete(task);
        await _taskRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static TaskResponse MapToResponse(TaskItem task)
    {
        return new TaskResponse(
            task.Id,
            task.Title,
            task.Description,
            task.Status,
            task.DueDate,
            task.UserId);
    }
}