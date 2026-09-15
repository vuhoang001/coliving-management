using Coliving.Application.DTOs;
using Coliving.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coliving.API.Controllers;

/// <summary>Danh mục dịch vụ thuê theo yêu cầu (giặt đồ, vệ sinh...).</summary>
[Authorize]
[Route("api/services")]
public class ServicesController : BaseApiController
{
    private readonly IServiceCatalogService _service;
    public ServicesController(IServiceCatalogService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<ServiceCatalogDto>>> GetAll([FromQuery] bool onlyActive = true)
        => Ok(await _service.GetAllAsync(onlyActive));

    [Authorize(Roles = "Manager,Admin")]
    [HttpPost]
    public async Task<ActionResult<ServiceCatalogDto>> Create(SaveServiceCatalogDto dto)
        => Ok(await _service.CreateAsync(dto));

    [Authorize(Roles = "Manager,Admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ServiceCatalogDto>> Update(int id, SaveServiceCatalogDto dto)
        => Ok(await _service.UpdateAsync(id, dto));

    [Authorize(Roles = "Manager,Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
