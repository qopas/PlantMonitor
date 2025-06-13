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

    protected IActionResult HandleResult<T>(ApiResponse<T> result)
    {
        if (result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    protected IActionResult HandleResult(ApiResponse result)
    {
        if (result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }
}
