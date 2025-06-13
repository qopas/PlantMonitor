using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantMonitor.Application.Common.Interfaces;
using PlantMonitor.Application.Features.Alerts.Commands;
using PlantMonitor.Application.Features.Alerts.Queries;
using PlantMonitor.Domain.Enums;

namespace PlantMonitor.Api.Controllers.v1;

[Authorize]
public class AlertsController(ICurrentUserService currentUserService) : BaseController
{
    /// <summary>
    /// Get alerts for current user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserAlerts([FromQuery] bool includeResolved = false)
    {
        if (!currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var query = new GetUserAlertsQuery(currentUserService.UserId.Value, includeResolved);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Get alerts for a specific device
    /// </summary>
    [HttpGet("device/{deviceId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDeviceAlerts(long deviceId, [FromQuery] bool includeResolved = false)
    {
        var query = new GetDeviceAlertsQuery(deviceId, includeResolved);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Create a device alert (typically called by ESP32)
    /// </summary>
    [HttpPost]
    [AllowAnonymous] // Devices use API tokens
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAlert([FromBody] CreateAlertRequest request)
    {
        var command = new CreateDeviceAlertCommand(
            request.DeviceId,
            request.AlertType,
            request.Severity,
            request.Title,
            request.Message);

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Resolve an alert
    /// </summary>
    [HttpPost("{alertId:long}/resolve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ResolveAlert(long alertId)
    {
        if (!currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var command = new ResolveAlertCommand(alertId, currentUserService.UserId.Value);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }
}

public class CreateAlertRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public AlertType AlertType { get; set; }
    public AlertSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
