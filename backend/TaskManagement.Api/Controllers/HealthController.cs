using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    private readonly IDatabaseHealthService _databaseHealthService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        IDatabaseHealthService databaseHealthService,
        ILogger<HealthController> logger)
    {
        _databaseHealthService = databaseHealthService;
        _logger = logger;
    }

    [HttpGet("database")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> CheckDatabase(
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _databaseHealthService.CheckAsync(
                cancellationToken);

            stopwatch.Stop();

            return Ok(new
            {
                status = "ready",
                durationMilliseconds = stopwatch.ElapsedMilliseconds
            });
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            return StatusCode(
                StatusCodes.Status504GatewayTimeout,
                new
                {
                    status = "timeout",
                    message =
                        "The database took too long to become available."
                });
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Database health check failed.");

            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new
                {
                    status = "unavailable",
                    message =
                        "The database is not ready yet."
                });
        }
    }
}