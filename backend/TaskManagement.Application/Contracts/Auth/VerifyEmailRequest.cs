namespace TaskManagement.Application.Contracts.Auth;

public sealed record VerifyEmailRequest(
    string Email,
    string Code);