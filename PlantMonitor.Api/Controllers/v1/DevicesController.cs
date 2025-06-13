using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantMonitor.Application.Common.Interfaces;
using PlantMonitor.Application.Features.Devices.Commands;
using PlantMonitor.Application.Features.Devices.Queries;

namespace PlantMonitor.Api.Controllers.v1;

[Authorize]
public class DevicesController : BaseController
{
    private readonly ICurrentUserService _currentUserService;

    public DevicesController(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Get all devices for the current user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserDevices()
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var query = new GetUserDevicesQuery(_currentUserService.UserId.Value);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Get device by device ID
    /// </summary>
    [HttpGet("{deviceId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDevice(string deviceId)
    {
        var query = new GetDeviceByDeviceIdQuery(deviceId);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Register a new device
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequest request)
    {
        var command = new RegisterDeviceCommand(request.DeviceId, request.UserEmail, request.DeviceName);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Update device information
    /// </summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateDevice(long id, [FromBody] UpdateDeviceRequest request)
    {
        var command = new UpdateDeviceCommand(id, request.DeviceName, request.Location, request.FirmwareVersion);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Update device online status (for device heartbeat)
    /// </summary>
    [HttpPost("{deviceId}/heartbeat")]
    [AllowAnonymous] // Devices use API tokens
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeviceHeartbeat(string deviceId)
    {
        var command = new UpdateDeviceStatusCommand(deviceId, true, DateTime.UtcNow);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Get device configuration for ESP32
    /// </summary>
    [HttpGet("{deviceId}/config")]
    [AllowAnonymous] // Devices use API tokens
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDeviceConfig(string deviceId)
    {
        var query = new GetDeviceConfigQuery(deviceId);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }
}

public class RegisterDeviceRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
}

public class UpdateDeviceRequest
{
    public string? DeviceName { get; set; }
    public string? Location { get; set; }
    public string? FirmwareVersion { get; set; }
}
