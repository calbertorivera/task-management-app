namespace TaskManagement.Application.Exceptions;

public sealed class EmailVerificationRateLimitException : Exception
{
    public int RetryAfterSeconds { get; }

    public EmailVerificationRateLimitException(
        int retryAfterSeconds)
        : base(
            "Please wait before requesting another verification email.")
    {
        RetryAfterSeconds = Math.Max(1, retryAfterSeconds);
    }
}