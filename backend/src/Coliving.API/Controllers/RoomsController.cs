using Coliving.Application.Common;
using Coliving.Application.DTOs;
using Coliving.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coliving.API.Controllers;

[Authorize]
public class RoomsController : BaseApiController
{
    private readonly IRoomService _service;
    public RoomsController(IRoomService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<PagedResult<RoomDto>>> Search([FromQuery] RoomFilterDto filter)
        => Ok(await _service.SearchAsync(filter));

    [HttpGet("available")]
    public async Task<ActionResult<List<RoomDto>>> Available([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Ok(await _service.GetAvailableAsync(from, to));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoomDto>> Get(int id) => Ok(await _service.GetByIdAsync(id));

    [Authorize(Roles = "Manager,Admin")]
    [HttpPost]
    public async Task<ActionResult<RoomDto>> Create(SaveRoomDto dto) => Ok(await _service.CreateAsync(dto));

    [Authorize(Roles = "Manager,Admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<RoomDto>> Update(int id, SaveRoomDto dto)
        => Ok(await _service.UpdateAsync(id, dto));

    [Authorize(Roles = "Manager,Admin,Staff")]
    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<RoomDto>> SetStatus(int id, [FromQuery] string value)
        => Ok(await _service.SetStatusAsync(id, value));

    [Authorize(Roles = "Manager,Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
