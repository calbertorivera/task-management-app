namespace TaskManagement.Application.Exceptions;

public sealed class EmailNotVerifiedException : Exception
{
    public EmailNotVerifiedException()
        : base("Please verify your email before logging in.")
    {
    }
}