using Microsoft.AspNetCore.Mvc;
using ZemplerTicketing.Contracts;
using ZemplerTicketing.Services;

namespace ZemplerTicketing.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly ITicketingService _service;

    public EventsController(ITicketingService service)
    {
        _service = service;
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetEvent(int id)
    {
        var result = await _service.GetEventDetailAsync(id);

        if (result is null)
            return NotFound(new ProblemDetails
            {
                Status = 404,
                Title = "Event not found",
                Detail = $"Event {id} does not exist."
            });

        return Ok(result);
    }

    [HttpPost("{id:int}/reserve")]
    public async Task<IActionResult> ReserveTicket(int id, [FromBody] ReserveRequest request)
    {
        var result = await _service.ReserveTicketAsync(id, request.HolderName);

        return result.StatusCode switch
        {
            201 => CreatedAtAction(nameof(GetEvent), new { id }, result.Value),
            404 => NotFound(ToProblem(result)),
            409 => Conflict(ToProblem(result)),
            _ => StatusCode(result.StatusCode, ToProblem(result))
        };
    }

    private static ProblemDetails ToProblem<T>(ServiceResult<T> result) => new()
    {
        Status = result.StatusCode,
        Title = result.StatusCode switch
        {
            404 => "Not found",
            409 => "Conflict",
            _ => "Error"
        },
        Detail = result.Error
    };
}
