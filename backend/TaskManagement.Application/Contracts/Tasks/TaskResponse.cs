using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Contracts.Tasks;

public sealed record TaskResponse(
    Guid Id,
    string Title,
    string Description,
    TaskItemStatus Status,
    DateTime? DueDate,
    Guid UserId);