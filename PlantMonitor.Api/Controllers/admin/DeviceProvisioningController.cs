using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantMonitor.Application.Common.Interfaces;
using System.Security.Cryptography;

namespace PlantMonitor.Api.Controllers.admin;

[ApiController]
[Route("api/admin/device-provisioning")]
[Authorize(Roles = "Admin,Manufacturer")]
public class DeviceProvisioningController : ControllerBase
{
    private readonly IDeviceProvisioningService _provisioningService;

    public DeviceProvisioningController(IDeviceProvisioningService provisioningService)
    {
        _provisioningService = provisioningService;
    }

    [HttpPost("provision")]
    public async Task<IActionResult> ProvisionDevice([FromBody] ProvisionDeviceRequest request)
    {
        var result = await _provisioningService.ProvisionNewDeviceAsync(request.DeviceId);
        
        if (result.Success)
        {
            return Ok(new 
            { 
                DeviceId = result.DeviceId,
                Token = result.Token,
                Message = "Device provisioned successfully"
            });
        }
        
        return BadRequest(result.ErrorMessage);
    }

    [HttpPost("batch-provision")]
    public async Task<IActionResult> BatchProvisionDevices([FromBody] BatchProvisionRequest request)
    {
        var results = new List<DeviceProvisioningResult>();
        
        foreach (var deviceId in request.DeviceIds)
        {
            var result = await _provisioningService.ProvisionNewDeviceAsync(deviceId);
            results.Add(result);
        }
        
        return Ok(results);
    }
}

public class ProvisionDeviceRequest
{
    public string DeviceId { get; set; } = string.Empty;
}

public class BatchProvisionRequest
{
    public List<string> DeviceIds { get; set; } = new();
}
