using Coliving.Application.Common;
using Coliving.Application.DTOs;
using Coliving.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coliving.API.Controllers;

[Authorize]
[Route("api/service-requests")]
public class ServiceRequestsController : BaseApiController
{
    private readonly IServiceRequestService _service;
    public ServiceRequestsController(IServiceRequestService service) => _service = service;

    [Authorize(Roles = "Manager,Admin,Staff")]
    [HttpGet]
    public async Task<ActionResult<PagedResult<ServiceRequestDto>>> GetAll([FromQuery] PaginationQuery query,
        [FromQuery] string? status)
        => Ok(await _service.GetAllAsync(query, status));

    [HttpGet("mine")]
    public async Task<ActionResult<List<ServiceRequestDto>>> Mine() => Ok(await _service.GetMineAsync(CurrentUserId));

    [HttpPost]
    public async Task<ActionResult<ServiceRequestDto>> Create(CreateServiceRequestDto dto)
        => Ok(await _service.CreateAsync(CurrentUserId, dto));

    [Authorize(Roles = "Manager,Admin,Staff")]
    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<ServiceRequestDto>> UpdateStatus(int id, [FromQuery] string status,
        [FromQuery] int? assignedToId)
        => Ok(await _service.UpdateStatusAsync(id, status, assignedToId));

    [HttpPut("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        await _service.CancelAsync(CurrentUserId, CurrentRole, id);
        return NoContent();
    }
}
