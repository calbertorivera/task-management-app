using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Contracts.Auth;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<RegistrationResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.RegisterAsync(
                request,
                cancellationToken);

            return Accepted(response);
        }
        catch (EmailDeliveryException exception)
        {
            Response.Headers.RetryAfter = "60";

            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new
                {
                    message = exception.Message
                });
        }
        catch (EmailVerificationRateLimitException exception)
        {
            return TooManyRequests(exception);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new
            {
                message = exception.Message
            });
        }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.LoginAsync(
                request,
                cancellationToken);

            return Ok(response);
        }
        catch (EmailNotVerifiedException exception)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new
                {
                    message = exception.Message
                });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new
            {
                message = exception.Message
            });
        }
    }

    [AllowAnonymous]
    [HttpPost("verify-email")]
    public async Task<ActionResult<AuthResponse>> VerifyEmail(
        VerifyEmailRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.VerifyEmailAsync(
                request,
                cancellationToken);

            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }

    [AllowAnonymous]
    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification(
        ResendVerificationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _authService.ResendVerificationCodeAsync(
                request,
                cancellationToken);

            return Accepted(new
            {
                message =
                    "If the account exists and is not verified, a new code has been sent."
            });
        }
        catch (EmailDeliveryException exception)
        {
            Response.Headers.RetryAfter = "60";

            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new
                {
                    message = exception.Message
                });
        }
        catch (EmailVerificationRateLimitException exception)
        {
            return TooManyRequests(exception);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }

    private ObjectResult TooManyRequests(
        EmailVerificationRateLimitException exception)
    {
        Response.Headers.RetryAfter =
            exception.RetryAfterSeconds.ToString();

        return StatusCode(
            StatusCodes.Status429TooManyRequests,
            new
            {
                message = exception.Message,
                retryAfterSeconds =
                    exception.RetryAfterSeconds
            });
    }
}