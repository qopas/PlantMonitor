using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantMonitor.Application.Features.Auth.Commands;

namespace PlantMonitor.Api.Controllers.v1;

public class DeviceTokensController : BaseController
{
    [HttpPost("generate")]
    [Authorize(Roles = "Admin,Manufacturer")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateToken([FromBody] GenerateTokenRequest request)
    {
        var command = new GenerateDeviceTokenCommand(request.DeviceId, request.TokenName);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpPost("validate")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidateToken([FromBody] ValidateTokenRequest request)
    {
        var command = new ValidateDeviceTokenCommand(request.Token, request.DeviceId);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpPost("revoke")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest request)
    {
        return Ok();
    }

    [HttpGet("device/{deviceId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDeviceTokens(string deviceId)
    {
        return Ok();
    }
}

public class GenerateTokenRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public string? TokenName { get; set; }
}

public class ValidateTokenRequest
{
    public string Token { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
}

public class RevokeTokenRequest
{
    public string Token { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
}
