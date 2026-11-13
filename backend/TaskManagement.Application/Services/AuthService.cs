using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using TaskManagement.Application.Contracts.Auth;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Services;

public sealed class AuthService : IAuthService
{
    private const int VerificationCodeExpirationMinutes = 10;

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailSender _emailSender;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher<User> passwordHasher,
        IJwtTokenService jwtTokenService,
        IEmailSender emailSender)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _emailSender = emailSender;
    }

    public async Task<RegistrationResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password) ||
            request.Password.Length < 8)
        {
            throw new ArgumentException(
                "Password must contain at least 8 characters.");
        }

        var existingUser = await _userRepository.GetByEmailAsync(
            email,
            cancellationToken);

        if (existingUser is not null)
        {
            throw new InvalidOperationException(
                "A user with this email already exists.");
        }

        var user = new User(email, string.Empty);

        var passwordHash = _passwordHasher.HashPassword(
            user,
            request.Password);

        user.SetPasswordHash(passwordHash);

        var verificationCode = GenerateVerificationCode();

        var verificationCodeHash = HashVerificationCode(
            verificationCode);

        user.SetEmailVerificationCode(
            verificationCodeHash,
            DateTime.UtcNow.AddMinutes(10));

        // Primero intentamos enviar el correo.
        await _emailSender.SendVerificationCodeAsync(
            email,
            verificationCode,
            cancellationToken);

        // Solo se guarda si el correo fue aceptado.
        await _userRepository.AddAsync(
            user,
            cancellationToken);

        await _userRepository.SaveChangesAsync(
            cancellationToken);

        return new RegistrationResponse(
            user.Email,
            "Registration successful. Check your email to verify your account.");
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);

        var user = await _userRepository.GetByEmailAsync(
            email,
            cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedAccessException(
                "Invalid email or password.");
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAccessException(
                "Invalid email or password.");
        }

        if (!user.IsEmailVerified)
        {
            throw new EmailNotVerifiedException();
        }

        return new AuthResponse(
            user.Id,
            user.Email,
            _jwtTokenService.GenerateToken(user));
    }

    public async Task<AuthResponse> VerifyEmailAsync(
        VerifyEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);
        var code = request.Code?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.");
        }

        if (code.Length != 6 ||
            code.Any(character => character < '0' || character > '9'))
        {
            throw new ArgumentException(
                "Verification code must contain 6 digits.");
        }

        var user = await _userRepository.GetByEmailAsync(
            email,
            cancellationToken);

        if (user is null ||
            user.IsEmailVerified ||
            string.IsNullOrWhiteSpace(user.EmailVerificationCodeHash) ||
            !user.EmailVerificationCodeExpiresAtUtc.HasValue ||
            user.EmailVerificationCodeExpiresAtUtc.Value <= DateTime.UtcNow ||
            !VerificationCodeMatches(
                code,
                user.EmailVerificationCodeHash))
        {
            throw new InvalidOperationException(
                "Invalid or expired verification code.");
        }

        user.MarkEmailAsVerified();

        await _userRepository.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            user.Id,
            user.Email,
            _jwtTokenService.GenerateToken(user));
    }

    public async Task ResendVerificationCodeAsync(
        ResendVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.");
        }

        var user = await _userRepository.GetByEmailAsync(
            email,
            cancellationToken);

        if (user is null || user.IsEmailVerified)
        {
            return;
        }

        var verificationCode = GenerateVerificationCode();

        user.SetEmailVerificationCode(
            HashVerificationCode(verificationCode),
            DateTime.UtcNow.AddMinutes(
                VerificationCodeExpirationMinutes));

        await _userRepository.SaveChangesAsync(cancellationToken);

        await _emailSender.SendVerificationCodeAsync(
            user.Email,
            verificationCode,
            cancellationToken);
    }

    private static string NormalizeEmail(string? email)
    {
        return email?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private static string GenerateVerificationCode()
    {
        return RandomNumberGenerator
            .GetInt32(100000, 1000000)
            .ToString("D6", CultureInfo.InvariantCulture);
    }

    private static string HashVerificationCode(string code)
    {
        var hash = SHA256.HashData(
            Encoding.UTF8.GetBytes(code));

        return Convert.ToHexString(hash);
    }

    private static bool VerificationCodeMatches(
        string code,
        string storedHash)
    {
        try
        {
            var expectedHash = Convert.FromHexString(storedHash);
            var suppliedHash = SHA256.HashData(
                Encoding.UTF8.GetBytes(code));

            return CryptographicOperations.FixedTimeEquals(
                suppliedHash,
                expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}