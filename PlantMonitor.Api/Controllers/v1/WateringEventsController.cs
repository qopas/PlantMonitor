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
