namespace TaskManagement.Application.Contracts.Auth;

public sealed record RegistrationResponse(
    string Email,
    string Message);