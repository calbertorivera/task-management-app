using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TaskManagement.Api.Controllers;
using TaskManagement.Application.Contracts.Auth;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Exceptions;

namespace TaskManagement.Api.Tests.Controllers;

public sealed class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock = new();
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _controller = new AuthController(_authServiceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            },
        };
    }

    [Fact]
    public async Task Register_WhenSuccessful_ShouldReturnAccepted()
    {
        var response = new RegistrationResponse(
            "user@example.com",
            "Registration successful. Check your email to verify your account.");

        _authServiceMock
            .Setup(x => x.RegisterAsync(
                It.IsAny<RegisterRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.Register(
            new RegisterRequest("user@example.com", "Password123!"),
            CancellationToken.None);

        var acceptedResult = result.Result
            .Should()
            .BeOfType<AcceptedResult>()
            .Subject;

        acceptedResult.StatusCode
            .Should()
            .Be(StatusCodes.Status202Accepted);

        acceptedResult.Value
            .Should()
            .BeEquivalentTo(response);
    }

    [Fact]
    public async Task Register_WhenRequestIsInvalid_ShouldReturnBadRequest()
    {
        _authServiceMock
            .Setup(x => x.RegisterAsync(
                It.IsAny<RegisterRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Email is required."));

        var result = await _controller.Register(
            new RegisterRequest("", "Password123!"),
            CancellationToken.None);

        result.Result
            .Should()
            .BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Register_WhenEmailAlreadyExists_ShouldReturnConflict()
    {
        _authServiceMock
            .Setup(x => x.RegisterAsync(
                It.IsAny<RegisterRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(
                "A user with this email already exists."));

        var result = await _controller.Register(
            new RegisterRequest("user@example.com", "Password123!"),
            CancellationToken.None);

        var conflictResult = result.Result
            .Should()
            .BeOfType<ConflictObjectResult>()
            .Subject;

        conflictResult.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    [Fact]
    public async Task Login_WhenSuccessful_ShouldReturnOk()
    {
        var response = new AuthResponse(
            Guid.NewGuid(),
            "user@example.com",
            "jwt-token");

        _authServiceMock
            .Setup(x => x.LoginAsync(
                It.IsAny<LoginRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.Login(
            new LoginRequest("user@example.com", "Password123!"),
            CancellationToken.None);

        var okResult = result.Result
            .Should()
            .BeOfType<OkObjectResult>()
            .Subject;

        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task Login_WhenRequestIsInvalid_ShouldReturnBadRequest()
    {
        _authServiceMock
            .Setup(x => x.LoginAsync(
                It.IsAny<LoginRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException(
                "Password must contain at least 8 characters."));

        var result = await _controller.Login(
            new LoginRequest("user@example.com", "123"),
            CancellationToken.None);

        result.Result
            .Should()
            .BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Register_WhenEmailDeliveryFails_ShouldReturnServiceUnavailable()
    {
        _authServiceMock
            .Setup(x => x.RegisterAsync(
                It.IsAny<RegisterRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EmailDeliveryException());

        var result = await _controller.Register(
            new RegisterRequest(
                "user@example.com",
                "Password123!"),
            CancellationToken.None);

        var objectResult = result.Result
            .Should()
            .BeOfType<ObjectResult>()
            .Subject;

        objectResult.StatusCode
            .Should()
            .Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task Register_WhenRateLimitIsReached_ShouldReturnTooManyRequests()
    {
        _authServiceMock
            .Setup(x => x.RegisterAsync(
                It.IsAny<RegisterRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new EmailVerificationRateLimitException(45));

        var result = await _controller.Register(
            new RegisterRequest(
                "user@example.com",
                "Password123!"),
            CancellationToken.None);

        var objectResult = result.Result
            .Should()
            .BeOfType<ObjectResult>()
            .Subject;

        objectResult.StatusCode
            .Should()
            .Be(StatusCodes.Status429TooManyRequests);

        _controller.Response.Headers.RetryAfter
            .ToString()
            .Should()
            .Be("45");
    }

    [Fact]
    public async Task ResendVerification_WhenRateLimitIsReached_ShouldReturnTooManyRequests()
    {
        _authServiceMock
            .Setup(x => x.ResendVerificationCodeAsync(
                It.IsAny<ResendVerificationRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new EmailVerificationRateLimitException(30));

        var result = await _controller.ResendVerification(
            new ResendVerificationRequest(
                "user@example.com"),
            CancellationToken.None);

        var objectResult = result
            .Should()
            .BeOfType<ObjectResult>()
            .Subject;

        objectResult.StatusCode
            .Should()
            .Be(StatusCodes.Status429TooManyRequests);
    }

    [Fact]
    public async Task ResendVerification_WhenEmailDeliveryFails_ShouldReturnServiceUnavailable()
    {
        _authServiceMock
            .Setup(x => x.ResendVerificationCodeAsync(
                It.IsAny<ResendVerificationRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EmailDeliveryException());

        var result = await _controller.ResendVerification(
            new ResendVerificationRequest(
                "user@example.com"),
            CancellationToken.None);

        var objectResult = result
            .Should()
            .BeOfType<ObjectResult>()
            .Subject;

        objectResult.StatusCode
            .Should()
            .Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task VerifyEmail_WhenCodeIsInvalid_ShouldReturnBadRequest()
    {
        _authServiceMock
            .Setup(x => x.VerifyEmailAsync(
                It.IsAny<VerifyEmailRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new InvalidOperationException(
                    "Invalid or expired verification code."));

        var result = await _controller.VerifyEmail(
            new VerifyEmailRequest(
                "user@example.com",
                "000000"),
            CancellationToken.None);

        result.Result
            .Should()
            .BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Login_WhenCredentialsAreInvalid_ShouldReturnUnauthorized()
    {
        _authServiceMock
            .Setup(x => x.LoginAsync(
                It.IsAny<LoginRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException(
                "Invalid email or password."));

        var result = await _controller.Login(
            new LoginRequest("user@example.com", "WrongPassword"),
            CancellationToken.None);

        var unauthorizedResult = result.Result
            .Should()
            .BeOfType<UnauthorizedObjectResult>()
            .Subject;

        unauthorizedResult.StatusCode
            .Should()
            .Be(StatusCodes.Status401Unauthorized);
    }
}