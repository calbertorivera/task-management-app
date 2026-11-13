namespace TaskManagement.Application.Contracts.Auth;

public sealed record ResendVerificationRequest(
    string Email);