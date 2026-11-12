namespace TaskManagement.Application.Contracts.Auth;

public sealed record AuthResponse(
    Guid UserId,
    string Email,
    string Token);