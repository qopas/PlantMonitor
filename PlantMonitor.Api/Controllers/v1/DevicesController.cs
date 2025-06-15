using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantMonitor.Application.Common.Interfaces;
using PlantMonitor.Application.Features.Devices.Commands;
using PlantMonitor.Application.Features.Devices.Queries;

namespace PlantMonitor.Api.Controllers.v1;

public class DevicesController : BaseController
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDeviceCommandsService _deviceCommands;

    public DevicesController(ICurrentUserService currentUserService, IDeviceCommandsService deviceCommands)
    {
        _currentUserService = currentUserService;
        _deviceCommands = deviceCommands;
    }

    [HttpPost("{deviceId}/claim")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ClaimDevice(string deviceId, [FromBody] ClaimDeviceRequest request)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var command = new ClaimDeviceCommand(deviceId, _currentUserService.UserId.Value, 
            request.PlantName, request.PlantType, request.Location);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpGet]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
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

    [HttpPost("{deviceId}/heartbeat")]
    [Authorize(Policy = "DeviceApiKey")]
    [ProducesResponseType(StatusCodes.Status200OK)]
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

    [HttpGet("{deviceId}/commands")]
    [Authorize(Policy = "DeviceApiKey")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingCommands(string deviceId)
    {
        var authenticatedDeviceId = HttpContext.User.FindFirst("device_id")?.Value;
        if (authenticatedDeviceId != deviceId)
        {
            return Forbid("Device ID mismatch");
        }

        var commands = await _deviceCommands.GetPendingCommandsAsync(deviceId);
        
        var response = commands.Select(c => new
        {
            id = c.Id,
            commandType = c.CommandType.ToString(),
            parameters = c.Parameters,
            priority = c.Priority,
            createdAt = c.CreatedAt
        });

        return Ok(new { commands = response });
    }

    [HttpPost("commands/{commandId}/acknowledge")]
    [Authorize(Policy = "DeviceApiKey")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> AcknowledgeCommand(long commandId, [FromBody] AcknowledgeCommandRequest request)
    {
        var success = await _deviceCommands.AcknowledgeCommandAsync(
            commandId, request.Success, request.Result, request.ErrorMessage);

        if (success)
        {
            return Ok(new { message = "Command acknowledged" });
        }

        return NotFound(new { message = "Command not found" });
    }
}

public class ClaimDeviceRequest
{
    public string PlantName { get; set; } = string.Empty;
    public string PlantType { get; set; } = string.Empty;
    public string? Location { get; set; }
}

public class AcknowledgeCommandRequest
{
    public bool Success { get; set; }
    public string? Result { get; set; }
    public string? ErrorMessage { get; set; }
}
