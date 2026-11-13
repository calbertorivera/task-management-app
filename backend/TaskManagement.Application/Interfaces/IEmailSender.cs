namespace TaskManagement.Application.Interfaces;

public interface IEmailSender
{
    Task SendVerificationCodeAsync(
        string recipientEmail,
        string verificationCode,
        CancellationToken cancellationToken = default);
}