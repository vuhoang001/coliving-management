using Coliving.Application.Common;
using Coliving.Application.DTOs;
using Coliving.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coliving.API.Controllers;

[Authorize]
public class IncidentsController : BaseApiController
{
    private readonly IIncidentService _service;
    public IncidentsController(IIncidentService service) => _service = service;

    [Authorize(Roles = "Manager,Admin,Staff")]
    [HttpGet]
    public async Task<ActionResult<PagedResult<IncidentDto>>> GetAll([FromQuery] IncidentFilterDto filter)
        => Ok(await _service.GetAllAsync(filter));

    [HttpGet("mine")]
    public async Task<ActionResult<List<IncidentDto>>> Mine() => Ok(await _service.GetMineAsync(CurrentUserId));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<IncidentDto>> Get(int id) => Ok(await _service.GetByIdAsync(id));

    [HttpPost]
    public async Task<ActionResult<IncidentDto>> Create(CreateIncidentDto dto)
        => Ok(await _service.CreateAsync(CurrentUserId, dto));

    [Authorize(Roles = "Manager,Admin,Staff")]
    [HttpPut("{id:int}/assign")]
    public async Task<ActionResult<IncidentDto>> Assign(int id, [FromQuery] int staffId)
        => Ok(await _service.AssignAsync(id, staffId));

    [Authorize(Roles = "Manager,Admin,Staff")]
    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<IncidentDto>> UpdateStatus(int id, UpdateIncidentStatusDto dto)
        => Ok(await _service.UpdateStatusAsync(id, dto));
}
