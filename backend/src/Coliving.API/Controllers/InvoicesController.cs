using Coliving.Application.Common;
using Coliving.Application.DTOs;
using Coliving.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coliving.API.Controllers;

[Authorize]
public class InvoicesController : BaseApiController
{
    private readonly IInvoiceService _service;
    public InvoicesController(IInvoiceService service) => _service = service;

    [Authorize(Roles = "Manager,Admin,Staff")]
    [HttpGet]
    public async Task<ActionResult<PagedResult<InvoiceDto>>> GetAll([FromQuery] PaginationQuery query,
        [FromQuery] string? status)
        => Ok(await _service.GetAllAsync(query, status));

    [HttpGet("mine")]
    public async Task<ActionResult<List<InvoiceDto>>> Mine() => Ok(await _service.GetMineAsync(CurrentUserId));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InvoiceDto>> Get(int id)
        => Ok(await _service.GetByIdAsync(CurrentUserId, CurrentRole, id));

    [Authorize(Roles = "Manager,Admin,Staff")]
    [HttpPost]
    public async Task<ActionResult<InvoiceDto>> Create(CreateInvoiceDto dto) => Ok(await _service.CreateAsync(dto));

    [Authorize(Roles = "Manager,Admin,Staff")]
    [HttpPut("{id:int}/issue")]
    public async Task<ActionResult<InvoiceDto>> Issue(int id) => Ok(await _service.IssueAsync(id));

    [Authorize(Roles = "Manager,Admin,Staff")]
    [HttpPut("{id:int}/split")]
    public async Task<ActionResult<InvoiceDto>> Split(int id, SplitInvoiceDto dto)
        => Ok(await _service.SplitAsync(id, dto));

    [Authorize(Roles = "Manager,Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
