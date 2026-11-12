namespace TaskManagement.Domain.Entities;

public class TaskItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public TaskItemStatus Status { get; private set; } = TaskItemStatus.Pending;

    public DateTime? DueDate { get; private set; }

    public Guid UserId { get; private set; }

    public User User { get; private set; } = null!;

    private TaskItem()
    {
    }

    public TaskItem(
        string title,
        string description,
        DateTime? dueDate,
        Guid userId)
    {
        Title = title;
        Description = description;
        DueDate = dueDate;
        UserId = userId;
    }

    public void Update(
        string title,
        string description,
        TaskItemStatus status,
        DateTime? dueDate)
    {
        Title = title;
        Description = description;
        Status = status;
        DueDate = dueDate;
    }
}