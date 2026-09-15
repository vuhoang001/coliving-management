using Coliving.Application.DTOs;
using Coliving.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coliving.API.Controllers;

[Authorize]
public class ApartmentsController : BaseApiController
{
    private readonly IApartmentService _service;
    public ApartmentsController(IApartmentService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<ApartmentDto>>> GetAll([FromQuery] int? buildingId)
        => Ok(await _service.GetAllAsync(buildingId));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApartmentDto>> Get(int id) => Ok(await _service.GetByIdAsync(id));

    [Authorize(Roles = "Manager,Admin")]
    [HttpPost]
    public async Task<ActionResult<ApartmentDto>> Create(SaveApartmentDto dto) => Ok(await _service.CreateAsync(dto));

    [Authorize(Roles = "Manager,Admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApartmentDto>> Update(int id, SaveApartmentDto dto)
        => Ok(await _service.UpdateAsync(id, dto));

    [Authorize(Roles = "Manager,Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
