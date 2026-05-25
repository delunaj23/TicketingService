using Microsoft.AspNetCore.Mvc;
using ZemplerTicketing.Contracts;
using ZemplerTicketing.Services;

namespace ZemplerTicketing.Controllers;

[ApiController]
[Route("api/tickets")]
public class TicketsController : ControllerBase
{
    private readonly ITicketingService _service;

    public TicketsController(ITicketingService service)
    {
        _service = service;
    }

    [HttpPost("{id:int}/purchase")]
    public async Task<IActionResult> PurchaseTicket(int id, [FromBody] PurchaseRequest request)
    {
        var result = await _service.PurchaseTicketAsync(id, request.HolderName);

        return result.StatusCode switch
        {
            200 => Ok(result.Value),
            404 => NotFound(new ProblemDetails
            {
                Status = 404,
                Title = "Not found",
                Detail = result.Error
            }),
            409 => Conflict(new ProblemDetails
            {
                Status = 409,
                Title = "Conflict",
                Detail = result.Error
            }),
            _ => StatusCode(result.StatusCode, result.Error)
        };
    }
}
