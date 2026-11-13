namespace TaskManagement.Domain.Entities;

public class User
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public bool IsEmailVerified { get; private set; }

    public string? EmailVerificationCodeHash { get; private set; }

    public DateTime? EmailVerificationCodeExpiresAtUtc { get; private set; }

    public ICollection<TaskItem> Tasks { get; private set; } = new List<TaskItem>();

    private User()
    {
    }

    public User(string email, string passwordHash)
    {
        Email = email;
        PasswordHash = passwordHash;
        IsEmailVerified = false;
    }

    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
    }

    public void SetEmailVerificationCode(
        string codeHash,
        DateTime expiresAtUtc)
    {
        EmailVerificationCodeHash = codeHash;
        EmailVerificationCodeExpiresAtUtc = expiresAtUtc;
        IsEmailVerified = false;
    }

    public void MarkEmailAsVerified()
    {
        IsEmailVerified = true;
        EmailVerificationCodeHash = null;
        EmailVerificationCodeExpiresAtUtc = null;
    }
}