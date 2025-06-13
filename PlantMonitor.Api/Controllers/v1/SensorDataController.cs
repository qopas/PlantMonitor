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
