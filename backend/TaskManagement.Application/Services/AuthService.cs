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
    private const int VerificationEmailCooldownSeconds = 60;
    private const int MaxVerificationEmailsPerHour = 5;
    private const int MaxVerificationCodeAttempts = 5;

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

        ValidateRegistration(email, request.Password);

        var existingUser = await _userRepository.GetByEmailAsync(
            email,
            cancellationToken);

        if (existingUser is not null)
        {
            if (existingUser.IsEmailVerified)
            {
                throw new InvalidOperationException(
                    "A user with this email already exists.");
            }

            var resendTime = DateTime.UtcNow;

            EnsureVerificationEmailCanBeSent(
                existingUser,
                resendTime);

            var verificationCode = GenerateVerificationCode();

            existingUser.SetEmailVerificationCode(
                HashVerificationCode(verificationCode),
                resendTime.AddMinutes(
                    VerificationCodeExpirationMinutes));

            await _emailSender.SendVerificationCodeAsync(
                existingUser.Email,
                verificationCode,
                cancellationToken);

            existingUser.MarkVerificationEmailSent(resendTime);

            await _userRepository.SaveChangesAsync(
                cancellationToken);

            return new RegistrationResponse(
                existingUser.Email,
                "Registration successful. Check your email to verify your account.");
        }

        var user = new User(email, string.Empty);

        var passwordHash = _passwordHasher.HashPassword(
            user,
            request.Password);

        user.SetPasswordHash(passwordHash);

        var sendTime = DateTime.UtcNow;
        var newVerificationCode = GenerateVerificationCode();

        user.SetEmailVerificationCode(
            HashVerificationCode(newVerificationCode),
            sendTime.AddMinutes(
                VerificationCodeExpirationMinutes));

        await _emailSender.SendVerificationCodeAsync(
            email,
            newVerificationCode,
            cancellationToken);

        user.MarkVerificationEmailSent(sendTime);

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

        var verificationResult =
            _passwordHasher.VerifyHashedPassword(
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
            string.IsNullOrWhiteSpace(
                user.EmailVerificationCodeHash) ||
            !user.EmailVerificationCodeExpiresAtUtc.HasValue ||
            user.EmailVerificationCodeExpiresAtUtc.Value <=
                DateTime.UtcNow)
        {
            throw new InvalidOperationException(
                "Invalid or expired verification code.");
        }

        if (user.EmailVerificationCodeAttemptCount >=
            MaxVerificationCodeAttempts)
        {
            user.InvalidateEmailVerificationCode();

            await _userRepository.SaveChangesAsync(
                cancellationToken);

            throw new InvalidOperationException(
                "Invalid or expired verification code.");
        }

        if (!VerificationCodeMatches(
                code,
                user.EmailVerificationCodeHash))
        {
            user.RegisterInvalidVerificationAttempt();

            if (user.EmailVerificationCodeAttemptCount >=
                MaxVerificationCodeAttempts)
            {
                user.InvalidateEmailVerificationCode();
            }

            await _userRepository.SaveChangesAsync(
                cancellationToken);

            throw new InvalidOperationException(
                "Invalid or expired verification code.");
        }

        user.MarkEmailAsVerified();

        await _userRepository.SaveChangesAsync(
            cancellationToken);

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

        var sendTime = DateTime.UtcNow;

        EnsureVerificationEmailCanBeSent(
            user,
            sendTime);

        var verificationCode = GenerateVerificationCode();

        user.SetEmailVerificationCode(
            HashVerificationCode(verificationCode),
            sendTime.AddMinutes(
                VerificationCodeExpirationMinutes));

        await _emailSender.SendVerificationCodeAsync(
            user.Email,
            verificationCode,
            cancellationToken);

        user.MarkVerificationEmailSent(sendTime);

        await _userRepository.SaveChangesAsync(
            cancellationToken);
    }

    private static void ValidateRegistration(
        string email,
        string password)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(password) ||
            password.Length < 8)
        {
            throw new ArgumentException(
                "Password must contain at least 8 characters.");
        }
    }

    private static void EnsureVerificationEmailCanBeSent(
        User user,
        DateTime now)
    {
        if (user.EmailVerificationCodeSentAtUtc.HasValue)
        {
            var cooldownEndsAt =
                user.EmailVerificationCodeSentAtUtc.Value
                    .AddSeconds(VerificationEmailCooldownSeconds);

            if (cooldownEndsAt > now)
            {
                throw new EmailVerificationRateLimitException(
                    GetRetryAfterSeconds(cooldownEndsAt, now));
            }
        }

        if (user.EmailVerificationSendWindowStartedAtUtc.HasValue &&
            now <
                user.EmailVerificationSendWindowStartedAtUtc.Value
                    .AddHours(1) &&
            user.EmailVerificationSendCount >=
                MaxVerificationEmailsPerHour)
        {
            var windowEndsAt =
                user.EmailVerificationSendWindowStartedAtUtc.Value
                    .AddHours(1);

            throw new EmailVerificationRateLimitException(
                GetRetryAfterSeconds(windowEndsAt, now));
        }
    }

    private static int GetRetryAfterSeconds(
        DateTime retryAt,
        DateTime now)
    {
        return Math.Max(
            1,
            (int)Math.Ceiling(
                (retryAt - now).TotalSeconds));
    }

    private static string NormalizeEmail(string? email)
    {
        return email?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private static string GenerateVerificationCode()
    {
        return RandomNumberGenerator
            .GetInt32(100000, 1000000)
            .ToString(
                "D6",
                CultureInfo.InvariantCulture);
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