namespace TaskManagement.Application.Contracts.Tasks;

public sealed record CreateTaskRequest(
    string Title,
    string Description,
    DateTime? DueDate);