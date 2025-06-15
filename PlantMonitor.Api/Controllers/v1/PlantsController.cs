using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantMonitor.Application.Features.Plants.Commands;

namespace PlantMonitor.Api.Controllers.v1;

public class PlantsController : BaseController
{
    [HttpPost("{plantId}/water")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ManualWatering(long plantId, [FromBody] ManualWateringRequest? request = null)
    {
        var command = new ManualWateringCommand(plantId, request?.DurationSeconds);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpPut("{plantId}/config")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePlantConfig(long plantId, [FromBody] UpdatePlantConfigRequest request)
    {
        // TODO: Implement UpdatePlantConfigCommand
        return Ok(new { message = "Plant configuration updated" });
    }
}

public class ManualWateringRequest
{
    public int? DurationSeconds { get; set; }
}

public class UpdatePlantConfigRequest
{
    public string? PlantName { get; set; }
    public int? MoistureThresholdLow { get; set; }
    public int? MoistureThresholdHigh { get; set; }
    public int? WateringDuration { get; set; }
    public bool? AutoWateringEnabled { get; set; }
}
