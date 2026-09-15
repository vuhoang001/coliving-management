using Coliving.Application.Common;
using Coliving.Application.DTOs;
using Coliving.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coliving.API.Controllers;

[Authorize(Roles = "Manager,Admin,Staff")]
public class AssetsController : BaseApiController
{
    private readonly IAssetService _service;
    public AssetsController(IAssetService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<PagedResult<AssetDto>>> Search([FromQuery] AssetFilterDto filter)
        => Ok(await _service.SearchAsync(filter));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AssetDto>> Get(int id) => Ok(await _service.GetByIdAsync(id));

    [HttpPost]
    public async Task<ActionResult<AssetDto>> Create(SaveAssetDto dto) => Ok(await _service.CreateAsync(dto));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AssetDto>> Update(int id, SaveAssetDto dto)
        => Ok(await _service.UpdateAsync(id, dto));

    [Authorize(Roles = "Manager,Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
