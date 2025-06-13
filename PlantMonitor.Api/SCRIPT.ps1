# PowerShell script to create API layer structure
# Run this from the src/PlantMonitor.Api directory

Write-Host "Creating Plant Monitor API Layer Structure..." -ForegroundColor Green

# Create directory structure
$directories = @(
    "Controllers",
    "Controllers/v1",
    "Middleware",
    "Models/Requests",
    "Models/Responses"
)

foreach ($dir in $directories) {
    if (!(Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force
        Write-Host "Created directory: $dir" -ForegroundColor Yellow
    }
}

# Create Base Controller
@"
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PlantMonitor.Application.Common.Models;

namespace PlantMonitor.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public abstract class BaseController : ControllerBase
{
    private ISender? _mediator;
    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected ActionResult<ApiResponse<T>> HandleResult<T>(ApiResponse<T> result)
    {
        if (result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    protected ActionResult<ApiResponse> HandleResult(ApiResponse result)
    {
        if (result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }
}
"@ | Out-File -FilePath "Controllers/BaseController.cs" -Encoding UTF8

# Create Auth Controller
@"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PlantMonitor.Application.Features.Auth.Commands;
using PlantMonitor.Domain.Entities;
using PlantMonitor.Infrastructure.Authentication;

namespace PlantMonitor.Api.Controllers.v1;

[Route("api/v1/[controller]")]
public class AuthController : BaseController
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly JwtTokenService _jwtTokenService;

    public AuthController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        JwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Login user and get JWT token
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!result.Succeeded)
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }

        var token = await _jwtTokenService.GenerateTokenAsync(user);
        
        return Ok(new
        {
            success = true,
            data = new
            {
                token,
                user = new
                {
                    id = user.Id,
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName
                }
            },
            message = "Login successful"
        });
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized();
        }

        return Ok(new
        {
            success = true,
            data = new
            {
                id = user.Id,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                timeZone = user.TimeZone,
                lastLogin = user.LastLogin
            }
        });
    }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
"@ | Out-File -FilePath "Controllers/v1/AuthController.cs" -Encoding UTF8

# Create Devices Controller
@"
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
"@ | Out-File -FilePath "Controllers/v1/DevicesController.cs" -Encoding UTF8

# Create Plants Controller
@"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantMonitor.Application.Features.Plants.Commands;
using PlantMonitor.Application.Features.Plants.Queries;

namespace PlantMonitor.Api.Controllers.v1;

[Authorize]
public class PlantsController : BaseController
{
    /// <summary>
    /// Get plant by device ID
    /// </summary>
    [HttpGet("device/{deviceId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlantByDevice(long deviceId)
    {
        var query = new GetPlantByDeviceIdQuery(deviceId);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Get plant by ID
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlant(long id)
    {
        var query = new GetPlantByIdQuery(id);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Create a new plant configuration
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePlant([FromBody] CreatePlantRequest request)
    {
        var command = new CreatePlantCommand(
            request.DeviceId,
            request.PlantName,
            request.PlantType,
            request.PlantSpecies,
            request.AgeWeeks,
            request.MoistureThresholdLow,
            request.MoistureThresholdHigh,
            request.WateringDuration,
            request.AutoWateringEnabled,
            request.Notes);

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Update plant configuration
    /// </summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdatePlant(long id, [FromBody] UpdatePlantRequest request)
    {
        var command = new UpdatePlantCommand(
            id,
            request.PlantName,
            request.PlantType,
            request.MoistureThresholdLow,
            request.MoistureThresholdHigh,
            request.WateringDuration,
            request.AutoWateringEnabled,
            request.Notes);

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete a plant configuration
    /// </summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePlant(long id)
    {
        var command = new DeletePlantCommand(id);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }
}

public class CreatePlantRequest
{
    public long DeviceId { get; set; }
    public string PlantName { get; set; } = string.Empty;
    public string PlantType { get; set; } = string.Empty;
    public string? PlantSpecies { get; set; }
    public int? AgeWeeks { get; set; }
    public int MoistureThresholdLow { get; set; } = 30;
    public int MoistureThresholdHigh { get; set; } = 70;
    public int WateringDuration { get; set; } = 10;
    public bool AutoWateringEnabled { get; set; } = true;
    public string? Notes { get; set; }
}

public class UpdatePlantRequest
{
    public string? PlantName { get; set; }
    public string? PlantType { get; set; }
    public int? MoistureThresholdLow { get; set; }
    public int? MoistureThresholdHigh { get; set; }
    public int? WateringDuration { get; set; }
    public bool? AutoWateringEnabled { get; set; }
    public string? Notes { get; set; }
}
"@ | Out-File -FilePath "Controllers/v1/PlantsController.cs" -Encoding UTF8

# Create SensorData Controller
@"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantMonitor.Application.Features.SensorData.Commands;
using PlantMonitor.Application.Features.SensorData.Queries;

namespace PlantMonitor.Api.Controllers.v1;

public class SensorDataController : BaseController
{
    /// <summary>
    /// Record sensor data from ESP32 device
    /// </summary>
    [HttpPost]
    [AllowAnonymous] // Devices use API tokens
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordSensorData([FromBody] RecordSensorDataRequest request)
    {
        var command = new RecordSensorDataCommand(
            request.DeviceId,
            request.Timestamp,
            request.SoilMoisture,
            request.WaterLevel,
            request.SoilMoistureRaw,
            request.WaterLevelRaw,
            request.Temperature,
            request.IsValid);

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Get latest sensor data for a device
    /// </summary>
    [HttpGet("{deviceId}/latest")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLatestSensorData(string deviceId)
    {
        var query = new GetLatestSensorDataQuery(deviceId);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Get sensor data history for a device
    /// </summary>
    [HttpGet("{deviceId}/history")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSensorDataHistory(
        string deviceId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-7); // Default to last 7 days
        var to = toDate ?? DateTime.UtcNow;

        var query = new GetSensorDataHistoryQuery(deviceId, from, to);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }
}

public class RecordSensorDataRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public decimal SoilMoisture { get; set; }
    public decimal WaterLevel { get; set; }
    public int? SoilMoistureRaw { get; set; }
    public int? WaterLevelRaw { get; set; }
    public decimal? Temperature { get; set; }
    public bool IsValid { get; set; } = true;
}
"@ | Out-File -FilePath "Controllers/v1/SensorDataController.cs" -Encoding UTF8

# Create WateringEvents Controller
@"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantMonitor.Application.Features.WateringEvents.Commands;
using PlantMonitor.Application.Features.WateringEvents.Queries;
using PlantMonitor.Domain.Enums;

namespace PlantMonitor.Api.Controllers.v1;

public class WateringEventsController : BaseController
{
    /// <summary>
    /// Record watering event from ESP32 device
    /// </summary>
    [HttpPost]
    [AllowAnonymous] // Devices use API tokens
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordWateringEvent([FromBody] RecordWateringEventRequest request)
    {
        var command = new RecordWateringEventCommand(
            request.DeviceId,
            request.Timestamp,
            request.TriggerType,
            request.SoilMoistureBefore,
            request.SoilMoistureAfter,
            request.WaterLevelBefore,
            request.WaterLevelAfter,
            request.DurationSeconds,
            request.WasSuccessful,
            request.FailureReason);

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Get watering history for a device
    /// </summary>
    [HttpGet("{deviceId}/history")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWateringHistory(
        string deviceId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var query = new GetWateringHistoryQuery(deviceId, fromDate, toDate);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Get latest watering event for a device
    /// </summary>
    [HttpGet("{deviceId}/latest")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLatestWateringEvent(string deviceId)
    {
        var query = new GetLatestWateringEventQuery(deviceId);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Trigger manual watering (from mobile app)
    /// </summary>
    [HttpPost("{deviceId:long}/trigger")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TriggerWatering(long deviceId, [FromBody] TriggerWateringRequest? request = null)
    {
        var command = new TriggerWateringCommand(deviceId, request?.DurationSeconds);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }
}

public class RecordWateringEventRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public TriggerType TriggerType { get; set; }
    public decimal? SoilMoistureBefore { get; set; }
    public decimal? SoilMoistureAfter { get; set; }
    public decimal? WaterLevelBefore { get; set; }
    public decimal? WaterLevelAfter { get; set; }
    public int DurationSeconds { get; set; }
    public bool WasSuccessful { get; set; }
    public string? FailureReason { get; set; }
}

public class TriggerWateringRequest
{
    public int? DurationSeconds { get; set; }
}
"@ | Out-File -FilePath "Controllers/v1/WateringEventsController.cs" -Encoding UTF8

# Create Alerts Controller
@"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantMonitor.Application.Common.Interfaces;
using PlantMonitor.Application.Features.Alerts.Commands;
using PlantMonitor.Application.Features.Alerts.Queries;
using PlantMonitor.Domain.Enums;

namespace PlantMonitor.Api.Controllers.v1;

[Authorize]
public class AlertsController : BaseController
{
    private readonly ICurrentUserService _currentUserService;

    public AlertsController(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Get alerts for current user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserAlerts([FromQuery] bool includeResolved = false)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var query = new GetUserAlertsQuery(_currentUserService.UserId.Value, includeResolved);
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
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var command = new ResolveAlertCommand(alertId, _currentUserService.UserId.Value);
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
"@ | Out-File -FilePath "Controllers/v1/AlertsController.cs" -Encoding UTF8

# Create Device Token Controller
@"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantMonitor.Application.Features.Auth.Commands;

namespace PlantMonitor.Api.Controllers.v1;

[Authorize]
public class DeviceTokensController : BaseController
{
    /// <summary>
    /// Generate API token for device
    /// </summary>
    [HttpPost("generate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateToken([FromBody] GenerateTokenRequest request)
    {
        var command = new GenerateDeviceTokenCommand(request.DeviceId, request.TokenName);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Validate device token (used by devices)
    /// </summary>
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
"@ | Out-File -FilePath "Controllers/v1/DeviceTokensController.cs" -Encoding UTF8

# Create Exception Handling Middleware
@"
using System.Net;
using System.Text.Json;

namespace PlantMonitor.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            success = false,
            error = "An error occurred while processing your request",
            message = exception.Message
        };

        context.Response.StatusCode = exception switch
        {
            ArgumentException => (int)HttpStatusCode.BadRequest,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            _ => (int)HttpStatusCode.InternalServerError
        };

        var jsonResponse = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(jsonResponse);
    }
}
"@ | Out-File -FilePath "Middleware/ExceptionHandlingMiddleware.cs" -Encoding UTF8

# Create API Key Authentication Middleware
@"
using Microsoft.EntityFrameworkCore;
using PlantMonitor.Application.Common.Interfaces;

namespace PlantMonitor.Api.Middleware;

public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;

    public ApiKeyAuthenticationMiddleware(RequestDelegate next, ILogger<ApiKeyAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IDeviceTokenService tokenService, IApplicationDbContext dbContext)
    {
        // Skip API key validation for certain endpoints
        var path = context.Request.Path.Value?.ToLower();
        if (ShouldSkipApiKeyValidation(path))
        {
            await _next(context);
            return;
        }

        // Check for API key in Authorization header or query parameter
        var apiKey = GetApiKeyFromRequest(context);
        if (string.IsNullOrEmpty(apiKey))
        {
            await _next(context);
            return;
        }

        // Get device ID from the request
        var deviceId = GetDeviceIdFromRequest(context);
        if (string.IsNullOrEmpty(deviceId))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Device ID is required for API key authentication");
            return;
        }

        // Validate the API key
        var device = await dbContext.Devices
            .FirstOrDefaultAsync(d => d.DeviceId == deviceId);

        if (device == null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid device ID");
            return;
        }

        var isValid = await tokenService.ValidateTokenAsync(apiKey, device.Id);
        if (!isValid)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid API key");
            return;
        }

        // Add device information to context
        context.Items["DeviceId"] = deviceId;
        context.Items["Device"] = device;

        await _next(context);
    }

    private static bool ShouldSkipApiKeyValidation(string? path)
    {
        if (string.IsNullOrEmpty(path)) return true;

        var skipPaths = new[]
        {
            "/api/v1/auth/",
            "/swagger",
            "/health",
            "/api/v1/devices/register"
        };

        return skipPaths.Any(skipPath => path.StartsWith(skipPath));
    }

    private static string? GetApiKeyFromRequest(HttpContext context)
    {
        // Check Authorization header (Bearer token)
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }

        // Check X-API-Key header
        var apiKeyHeader = context.Request.Headers["X-API-Key"].FirstOrDefault();
        if (!string.IsNullOrEmpty(apiKeyHeader))
        {
            return apiKeyHeader;
        }

        // Check query parameter
        return context.Request.Query["apiKey"].FirstOrDefault();
    }

    private static string? GetDeviceIdFromRequest(HttpContext context)
    {
        // Try to get device ID from route
        if (context.Request.RouteValues.TryGetValue("deviceId", out var routeDeviceId))
        {
            return routeDeviceId?.ToString();
        }

        // Try to get from request body (for POST requests)
        if (context.Request.HasJsonContentType() && context.Request.ContentLength > 0)
        {
            // This is a simplified approach - in a real implementation,
            // you might want to peek at the request body more carefully
            var deviceIdHeader = context.Request.Headers["X-Device-ID"].FirstOrDefault();
            if (!string.IsNullOrEmpty(deviceIdHeader))
            {
                return deviceIdHeader;
            }
        }

        // Try to get from query parameter
        return context.Request.Query["deviceId"].FirstOrDefault();
    }
}
"@ | Out-File -FilePath "Middleware/ApiKeyAuthenticationMiddleware.cs" -Encoding UTF8

# Create Program.cs
@"
using Microsoft.OpenApi.Models;
using PlantMonitor.Api.Middleware;
using PlantMonitor.Application;
using PlantMonitor.Infrastructure;
using Serilog;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();

// Add Application and Infrastructure layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Plant Monitor API",
        Version = "v1",
        Description = "API for Plant Monitoring System with ESP32 integration",
        Contact = new OpenApiContact
        {
            Name = "Plant Monitor Team",
            Email = "support@plantmonitor.com"
        }
    });

    // Include XML comments
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Add JWT Bearer authentication
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // Add API Key authentication for devices
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API Key for device authentication. Use X-API-Key header or apiKey query parameter.",
        Name = "X-API-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactNativeApp", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddDbContext<PlantMonitor.Infrastructure.Data.ApplicationDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Plant Monitor API V1");
        c.RoutePrefix = string.Empty; // Set Swagger UI as the root
    });
}

// Initialize database
try
{
    await app.Services.InitializeDatabaseAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "An error occurred while initializing the database");
    throw;
}

app.UseHttpsRedirection();

app.UseCors("AllowReactNativeApp");

// Add custom middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<ApiKeyAuthenticationMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Add health check endpoint
app.MapHealthChecks("/health");

// Add a simple status endpoint
app.MapGet("/", () => new
{
    service = "Plant Monitor API",
    version = "1.0.0",
    status = "Running",
    timestamp = DateTime.UtcNow
});

try
{
    Log.Information("Starting Plant Monitor API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
"@ | Out-File -FilePath "Program.cs" -Encoding UTF8

# Create appsettings.json
@"
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=PlantMonitorDb;Trusted_Connection=true;MultipleActiveResultSets=true;"
  },
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "PlantMonitorApi",
    "Audience": "PlantMonitorApp",
    "ExpiryInHours": "24"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/plantmonitor-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      }
    ]
  },
  "AllowedHosts": "*"
}
"@ | Out-File -FilePath "appsettings.json" -Encoding UTF8

# Create appsettings.Development.json
@"
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Information",
        "System": "Information"
      }
    }
  }
}
"@ | Out-File -FilePath "appsettings.Development.json" -Encoding UTF8

# Create .gitignore
@"
## Ignore Visual Studio temporary files, build results, and
## files generated by popular Visual Studio add-ons.

# User-specific files
*.rsuser
*.suo
*.user
*.userosscache
*.sln.docstates

# Build results
[Dd]ebug/
[Dd]ebugPublic/
[Rr]elease/
[Rr]eleases/
x64/
x86/
[Aa][Rr][Mm]/
[Aa][Rr][Mm]64/
bld/
[Bb]in/
[Oo]bj/
[Ll]og/

# Visual Studio 2015/2017 cache/options directory
.vs/

# MSTest test Results
[Tt]est[Rr]esult*/
[Bb]uild[Ll]og.*

# NUnit
*.VisualState.xml
TestResult.xml

# .NET Core
project.lock.json
project.fragment.lock.json
artifacts/

# ASP.NET Scaffolding
ScaffoldingReadMe.txt

# StyleCop
StyleCopReport.xml

# Files built by Visual Studio
*_i.c
*_p.c
*_h.h
*.ilk
*.meta
*.obj
*.iobj
*.pch
*.pdb
*.ipdb
*.pgc
*.pgd
*.rsp
*.sbr
*.tlb
*.tli
*.tlh
*.tmp
*.tmp_proj
*_wpftmp.csproj
*.log
*.vspscc
*.vssscc
.builds
*.pidb
*.svclog
*.scc

# Chutzpah Test files
_Chutzpah*

# Visual C++ cache files
ipch/
*.aps
*.ncb
*.opendb
*.opensdf
*.sdf
*.cachefile
*.VC.db
*.VC.VC.opendb

# Visual Studio profiler
*.psess
*.vsp
*.vspx
*.sap

# Visual Studio Trace Files
*.e2e

# TFS 2012 Local Workspace
$tf/

# Guidance Automation Toolkit
*.gpState

# ReSharper is a .NET coding add-in
_ReSharper*/
*.[Rr]e[Ss]harper
*.DotSettings.user

# JustCode is a .NET coding add-in
.JustCode

# TeamCity is a build add-in
_TeamCity*

# DotCover is a Code Coverage Tool
*.dotCover

# AxoCover is a Code Coverage Tool
.axoCover/*
!.axoCover/settings.json

# Visual Studio code coverage results
*.coverage
*.coveragexml

# NCrunch
_NCrunch_*
.*crunch*.local.xml
nCrunchTemp_*

# MightyMoose
*.mm.*
AutoTest.Net/

# Web workbench (sass)
.sass-cache/

# Installshield output folder
[Ee]xpress/

# DocProject is a documentation generator add-in
DocProject/buildhelp/
DocProject/Help/*.HxT
DocProject/Help/*.HxC
DocProject/Help/*.hhc
DocProject/Help/*.hhk
DocProject/Help/*.hhp
DocProject/Help/Html2
DocProject/Help/html

# Click-Once directory
publish/

# Publish Web Output
*.[Pp]ublish.xml
*.azurePubxml
# Note: Comment the next line if you want to checkin your web deploy settings,
# but database connection strings (with potential passwords) will be unencrypted
*.pubxml
*.publishproj

# Microsoft Azure Web App publish settings. Comment the next line if you want to
# checkin your Azure Web App publish settings, but sensitive information contained
# in these files may be revealed
*.azurePubxml

# Microsoft Azure Build Output
csx/
*.build.csdef

# Microsoft Azure Emulator
ecf/
rcf/

# Windows Store app package directories and files
AppPackages/
BundleArtifacts/
Package.StoreAssociation.xml
_pkginfo.txt
*.appx
*.appxbundle
*.appxupload

# Visual Studio cache files
# files ending in .cache can be ignored
*.[Cc]ache
# but keep track of directories ending in .cache
!?*.[Cc]ache/

# Others
ClientBin/
~$*
*~
*.dbmdl
*.dbproj.schemaview
*.jfm
*.pfx
*.publishsettings
orleans.codegen.cs

# Including strong name files can present a security risk
# (https://github.com/github/gitignore/pull/2483#issue-259490424)
#*.snk

# Since there are multiple workflows, uncomment next line to ignore bower_components
# (https://github.com/github/gitignore/pull/1529#issuecomment-104372622)
#bower_components/

# RIA/Silverlight projects
Generated_Code/

# Backup & report files from converting an old project file
# to a newer Visual Studio version. Backup files are not needed,
# because we have git ;-)
_UpgradeReport_Files/
Backup*/
UpgradeLog*.XML
UpgradeLog*.htm
CSharp__UpgradeReport_Files/

# SQL Server files
*.mdf
*.ldf
*.ndf

# Business Intelligence projects
*.rdl.data
*.bim.layout
*.bim_*.settings
*.rptproj.rsuser
*- [Bb]ackup.rdl
*- [Bb]ackup ([0-9]).rdl
*- [Bb]ackup ([0-9][0-9]).rdl

# Microsoft Fakes
FakesAssemblies/

# GhostDoc plugin setting file
*.GhostDoc.xml

# Node.js Tools for Visual Studio
.ntvs_analysis.dat
node_modules/

# Visual Studio 6 build log
*.plg

# Visual Studio 6 workspace options file
*.opt

# Visual Studio 6 auto-generated workspace file (contains which files were open etc.)
*.vbw

# Visual Studio LightSwitch build output
**/*.HTMLClient/GeneratedArtifacts
**/*.DesktopClient/GeneratedArtifacts
**/*.DesktopClient/ModelManifest.xml
**/*.Server/GeneratedArtifacts
**/*.Server/ModelManifest.xml
_Pvt_Extensions

# Paket dependency manager
.paket/paket.exe
paket-files/

# FAKE - F# Make
.fake/

# CodeRush personal settings
.cr/personal

# Python Tools for Visual Studio (PTVS)
__pycache__/
*.pyc

# Cake - Uncomment if you are using it
# tools/**
# !tools/packages.config

# Tabs Studio
*.tss

# Telerik's JustMock configuration file
*.jmconfig

# BizTalk build output
*.btp.cs
*.btm.cs
*.odx.cs
*.xsd.cs

# OpenCover UI analysis results
OpenCover/

# Azure Stream Analytics local run output
ASALocalRun/

# MSBuild Binary and Structured Log
*.binlog

# NVidia Nsight GPU debugger configuration file
*.nvuser

# MFractors (Xamarin productivity tool) working folder
.mfractor/

# Local History for Visual Studio
.localhistory/

# BeatPulse healthcheck temp database
healthchecksdb

# Backup folder for Package Reference Convert tool in Visual Studio 2017
MigrationBackup/

# Ionide (cross platform F# VS Code tools) working folder
.ionide/

# Logs
logs/
*.log
"@ | 
Out-File -FilePath ".gitignore" -Encoding UTF8
