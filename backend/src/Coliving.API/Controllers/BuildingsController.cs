using Coliving.Application.DTOs;
using Coliving.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coliving.API.Controllers;

[Authorize]
public class BuildingsController : BaseApiController
{
    private readonly IBuildingService _service;
    public BuildingsController(IBuildingService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<BuildingDto>>> GetAll() => Ok(await _service.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BuildingDto>> Get(int id) => Ok(await _service.GetByIdAsync(id));

    [Authorize(Roles = "Manager,Admin")]
    [HttpPost]
    public async Task<ActionResult<BuildingDto>> Create(SaveBuildingDto dto) => Ok(await _service.CreateAsync(dto));

    [Authorize(Roles = "Manager,Admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<BuildingDto>> Update(int id, SaveBuildingDto dto)
        => Ok(await _service.UpdateAsync(id, dto));

    [Authorize(Roles = "Manager,Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
