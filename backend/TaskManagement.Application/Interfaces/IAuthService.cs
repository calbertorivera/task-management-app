using TaskManagement.Application.Contracts.Auth;

namespace TaskManagement.Application.Interfaces;

public interface IAuthService
{
    Task<RegistrationResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);

    Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    Task<AuthResponse> VerifyEmailAsync(
        VerifyEmailRequest request,
        CancellationToken cancellationToken = default);

    Task ResendVerificationCodeAsync(
        ResendVerificationRequest request,
        CancellationToken cancellationToken = default);
}