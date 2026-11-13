namespace TaskManagement.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string ConnectionString { get; set; } = string.Empty;

    public string From { get; set; } = string.Empty;
}