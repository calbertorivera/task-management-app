using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Infrastructure.Email;

public sealed class AzureEmailSender : IEmailSender
{
    private readonly EmailClient _emailClient;
    private readonly EmailOptions _options;
    private readonly ILogger<AzureEmailSender> _logger;

    public AzureEmailSender(
        EmailClient emailClient,
        IOptions<EmailOptions> options,
        ILogger<AzureEmailSender> logger)
    {
        _emailClient = emailClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendVerificationCodeAsync(
        string recipientEmail,
        string verificationCode,
        CancellationToken cancellationToken = default)
    {
        var content = new EmailContent(
            "Verify your Task Management account")
        {
            PlainText = $"""
                Your Task Management verification code is:
                {verificationCode}

                This code expires in 10 minutes.
                """,

            Html = $"""
                <html>
                  <body style="font-family: Arial, sans-serif;">
                    <h2>Verify your Task Management account</h2>
                    <p>Your verification code is:</p>
                    <p style="font-size: 32px; font-weight: bold; letter-spacing: 8px;">
                      {verificationCode}
                    </p>
                    <p>This code expires in 10 minutes.</p>
                  </body>
                </html>
                """
        };

        var message = new EmailMessage(
            _options.From,
            recipientEmail,
            content);

        try
        {
            await _emailClient.SendAsync(
                WaitUntil.Completed,
                message,
                cancellationToken);
        }
        catch (RequestFailedException exception)
        {
            _logger.LogError(
                exception,
                "Azure Communication Services failed to send a verification email. ErrorCode: {ErrorCode}",
                exception.ErrorCode);

            throw new EmailDeliveryException(exception);
        }
    }
}