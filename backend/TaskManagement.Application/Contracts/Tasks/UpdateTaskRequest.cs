using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Contracts.Tasks;

public sealed record UpdateTaskRequest(
    string Title,
    string Description,
    TaskItemStatus Status,
    DateTime? DueDate);