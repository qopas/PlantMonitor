using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantMonitor.Application.Common.Interfaces;
using PlantMonitor.Application.Features.Devices.Commands;
using PlantMonitor.Application.Features.Devices.Queries;

namespace PlantMonitor.Api.Controllers.v1;

public class DevicesController : BaseController
{
    private readonly ICurrentUserService _currentUserService;

    public DevicesController(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [Authorize]
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

    [HttpGet("{deviceId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDevice(string deviceId)
    {
        var query = new GetDeviceByDeviceIdQuery(deviceId);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpPost("register")]
    [Authorize(Policy = "DeviceApiKey")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequest request)
    {
        var authenticatedDeviceId = HttpContext.User.FindFirst("device_id")?.Value;
        
        if (string.IsNullOrEmpty(authenticatedDeviceId) || authenticatedDeviceId != request.DeviceId)
        {
            return BadRequest("Device ID mismatch with authentication token");
        }

        var command = new RegisterDeviceCommand(request.DeviceId, request.UserEmail, request.DeviceName);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpPut("{id:long}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateDevice(long id, [FromBody] UpdateDeviceRequest request)
    {
        var command = new UpdateDeviceCommand(id, request.DeviceName, request.Location, request.FirmwareVersion);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpPost("{deviceId}/heartbeat")]
    [Authorize(Policy = "DeviceApiKey")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeviceHeartbeat(string deviceId)
    {
        var authenticatedDeviceId = HttpContext.User.FindFirst("device_id")?.Value;
        if (authenticatedDeviceId != deviceId)
        {
            return Forbid("Device ID mismatch");
        }

        var command = new UpdateDeviceStatusCommand(deviceId, true, DateTime.UtcNow);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpGet("{deviceId}/config")]
    [Authorize(Policy = "DeviceApiKey")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDeviceConfig(string deviceId)
    {
        var authenticatedDeviceId = HttpContext.User.FindFirst("device_id")?.Value;
        if (authenticatedDeviceId != deviceId)
        {
            return Forbid("Device ID mismatch");
        }

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
