using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using TaskManagement.Application.Contracts.Auth;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Entities;
using System.Security.Cryptography;
using System.Text;
using TaskManagement.Application.Exceptions;

namespace TaskManagement.Application.Tests.Services;

public sealed class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordHasher<User>> _passwordHasherMock = new();
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock = new();

    private readonly Mock<IEmailSender> _emailSenderMock = new();

    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _service = new AuthService(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _jwtTokenServiceMock.Object,
            _emailSenderMock.Object);


    }

    [Fact]
    public async Task RegisterAsync_WithValidRequest_ShouldCreateUser()
    {
        var request = new RegisterRequest(
            "  USER@Example.COM  ",
            "Password123!");

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(
                "user@example.com",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _passwordHasherMock
            .Setup(x => x.HashPassword(
                It.IsAny<User>(),
                request.Password))
            .Returns("hashed-password");

        _emailSenderMock
    .Setup(x => x.SendVerificationCodeAsync(
        "user@example.com",
        It.IsAny<string>(),
        It.IsAny<CancellationToken>()))
    .Returns(Task.CompletedTask);


        _userRepositoryMock
            .Setup(x => x.AddAsync(
                It.IsAny<User>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _userRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.RegisterAsync(request);

        result.Email.Should().Be("user@example.com");

        result.Message.Should().Be(
            "Registration successful. Check your email to verify your account.");

        _userRepositoryMock.Verify(x => x.AddAsync(
            It.Is<User>(user =>
                user.Email == "user@example.com" &&
                user.PasswordHash == "hashed-password"),
            It.IsAny<CancellationToken>()), Times.Once);

        _emailSenderMock.Verify(
    x => x.SendVerificationCodeAsync(
        "user@example.com",
        It.Is<string>(code =>
            code.Length == 6 &&
            code.All(char.IsDigit)),
        It.IsAny<CancellationToken>()),
    Times.Once);

        _userRepositoryMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WhenUnverifiedEmailAlreadyExists_ShouldSendNewCode()
    {
        var existingUser = new User(
            "user@example.com",
            "existing-hash");

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(
                "user@example.com",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _emailSenderMock
            .Setup(x => x.SendVerificationCodeAsync(
                "user@example.com",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _userRepositoryMock
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var request = new RegisterRequest(
            "user@example.com",
            "Password123!");

        var result = await _service.RegisterAsync(request);

        result.Email.Should().Be("user@example.com");

        result.Message.Should().Be(
            "Registration successful. Check your email to verify your account.");

        _userRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<User>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _emailSenderMock.Verify(
            x => x.SendVerificationCodeAsync(
                "user@example.com",
                It.Is<string>(code =>
                    code.Length == 6 &&
                    code.All(char.IsDigit)),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _userRepositoryMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WhenVerifiedEmailAlreadyExists_ShouldThrowException()
    {
        var existingUser = new User(
            "user@example.com",
            "existing-hash");

        existingUser.MarkEmailAsVerified();

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(
                "user@example.com",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        var request = new RegisterRequest(
            "user@example.com",
            "Password123!");

        Func<Task> action = () =>
            _service.RegisterAsync(request);

        await action.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("A user with this email already exists.");

        _emailSenderMock.Verify(
            x => x.SendVerificationCodeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _userRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<User>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WhenPasswordIsTooShort_ShouldThrowException()
    {
        var request = new RegisterRequest(
            "user@example.com",
            "1234567");

        Func<Task> action = () =>
            _service.RegisterAsync(request);

        await action.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Password must contain at least 8 characters.");
    }

    

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnAuthResponse()
    {
        var user = new User(
            "user@example.com",
            "hashed-password");

        user.MarkEmailAsVerified();

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(
                "user@example.com",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.VerifyHashedPassword(
                user,
                user.PasswordHash,
                "Password123!"))
            .Returns(PasswordVerificationResult.Success);

        _jwtTokenServiceMock
            .Setup(x => x.GenerateToken(user))
            .Returns("jwt-token");

        var request = new LoginRequest(
            " USER@EXAMPLE.COM ",
            "Password123!");

        var result = await _service.LoginAsync(request);

        result.UserId.Should().Be(user.Id);
        result.Email.Should().Be("user@example.com");
        result.Token.Should().Be("jwt-token");
    }

    [Fact]
    public async Task LoginAsync_WhenUserDoesNotExist_ShouldThrowUnauthorized()
    {
        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(
                "user@example.com",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var request = new LoginRequest(
            "user@example.com",
            "Password123!");

        Func<Task> action = () =>
            _service.LoginAsync(request);

        await action.Should()
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid email or password.");
    }

    private static string HashVerificationCode(string code)
    {
        return Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(code)));
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailSenderFails_ShouldNotCreateUser()
    {
        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(
                "user@example.com",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _passwordHasherMock
            .Setup(x => x.HashPassword(
                It.IsAny<User>(),
                "Password123!"))
            .Returns("hashed-password");

        _emailSenderMock
            .Setup(x => x.SendVerificationCodeAsync(
                "user@example.com",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EmailDeliveryException());

        var request = new RegisterRequest(
            "user@example.com",
            "Password123!");

        Func<Task> action = () =>
            _service.RegisterAsync(request);

        await action.Should()
            .ThrowAsync<EmailDeliveryException>();

        _userRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<User>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _userRepositoryMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResendVerificationCodeAsync_WhenCooldownHasNotElapsed_ShouldThrowRateLimit()
    {
        var user = new User(
            "user@example.com",
            "hashed-password");

        user.MarkVerificationEmailSent(DateTime.UtcNow);

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(
                "user@example.com",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        Func<Task> action = () =>
            _service.ResendVerificationCodeAsync(
                new ResendVerificationRequest(
                    "user@example.com"));

        var exception = await action.Should()
            .ThrowAsync<EmailVerificationRateLimitException>();

        exception.Which.RetryAfterSeconds
            .Should()
            .BeInRange(1, 60);

        _emailSenderMock.Verify(
            x => x.SendVerificationCodeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResendVerificationCodeAsync_WhenHourlyLimitIsReached_ShouldThrowRateLimit()
    {
        var user = new User(
            "user@example.com",
            "hashed-password");

        var previousSend = DateTime.UtcNow.AddMinutes(-2);

        for (var index = 0; index < 5; index++)
        {
            user.MarkVerificationEmailSent(previousSend);
        }

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(
                "user@example.com",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        Func<Task> action = () =>
            _service.ResendVerificationCodeAsync(
                new ResendVerificationRequest(
                    "user@example.com"));

        await action.Should()
            .ThrowAsync<EmailVerificationRateLimitException>();

        _emailSenderMock.Verify(
            x => x.SendVerificationCodeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task VerifyEmailAsync_WithValidCode_ShouldMarkEmailAsVerified()
    {
        const string verificationCode = "123456";

        var user = new User(
            "user@example.com",
            "hashed-password");

        user.SetEmailVerificationCode(
            HashVerificationCode(verificationCode),
            DateTime.UtcNow.AddMinutes(10));

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(
                "user@example.com",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _jwtTokenServiceMock
            .Setup(x => x.GenerateToken(user))
            .Returns("jwt-token");

        var result = await _service.VerifyEmailAsync(
            new VerifyEmailRequest(
                "user@example.com",
                verificationCode));

        result.Email.Should().Be("user@example.com");
        result.Token.Should().Be("jwt-token");
        user.IsEmailVerified.Should().BeTrue();
        user.EmailVerificationCodeHash.Should().BeNull();
    }

    [Fact]
    public async Task VerifyEmailAsync_WhenCodeIsInvalidFiveTimes_ShouldInvalidateCode()
    {
        const string validCode = "123456";
        const string invalidCode = "654321";

        var user = new User(
            "user@example.com",
            "hashed-password");

        user.SetEmailVerificationCode(
            HashVerificationCode(validCode),
            DateTime.UtcNow.AddMinutes(10));

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(
                "user@example.com",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            Func<Task> action = () =>
                _service.VerifyEmailAsync(
                    new VerifyEmailRequest(
                        "user@example.com",
                        invalidCode));

            await action.Should()
                .ThrowAsync<InvalidOperationException>();
        }

        user.EmailVerificationCodeHash.Should().BeNull();

        _userRepositoryMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Exactly(5));
    }

    [Fact]
    public async Task VerifyEmailAsync_WhenCodeIsNotNumeric_ShouldThrowArgumentException()
    {
        var request = new VerifyEmailRequest(
            "user@example.com",
            "12A456");

        Func<Task> action = () =>
            _service.VerifyEmailAsync(request);

        await action.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Verification code must contain 6 digits.");

        _userRepositoryMock.Verify(
            x => x.GetByEmailAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordIsInvalid_ShouldThrowUnauthorized()
    {
        var user = new User(
            "user@example.com",
            "hashed-password");

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(
                "user@example.com",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.VerifyHashedPassword(
                user,
                user.PasswordHash,
                "WrongPassword"))
            .Returns(PasswordVerificationResult.Failed);

        var request = new LoginRequest(
            "user@example.com",
            "WrongPassword");

        Func<Task> action = () =>
            _service.LoginAsync(request);

        await action.Should()
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid email or password.");
    }
}