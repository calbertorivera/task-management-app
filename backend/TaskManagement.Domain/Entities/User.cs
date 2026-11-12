namespace TaskManagement.Domain.Entities;

public class User
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public ICollection<TaskItem> Tasks { get; private set; } = new List<TaskItem>();

    private User()
    {
    }

    public User(string email, string passwordHash)
    {
        Email = email;
        PasswordHash = passwordHash;
    }

    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
    }
}