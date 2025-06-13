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
