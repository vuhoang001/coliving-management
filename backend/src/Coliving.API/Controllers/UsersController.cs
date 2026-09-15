using Coliving.Application.Common;
using Coliving.Application.DTOs;
using Coliving.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coliving.API.Controllers;

[Authorize(Roles = "Manager,Admin")]
public class UsersController : BaseApiController
{
    private readonly IUserService _service;
    public UsersController(IUserService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<PagedResult<UserDto>>> GetAll([FromQuery] PaginationQuery query,
        [FromQuery] string? role, [FromQuery] string? keyword)
        => Ok(await _service.GetAllAsync(query, role, keyword));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> Get(int id) => Ok(await _service.GetByIdAsync(id));

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(CreateUserDto dto) => Ok(await _service.CreateAsync(dto));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserDto>> Update(int id, UpdateUserDto dto)
        => Ok(await _service.UpdateAsync(id, dto));

    [HttpPut("{id:int}/active")]
    public async Task<IActionResult> SetActive(int id, [FromQuery] bool value)
    {
        await _service.SetActiveAsync(id, value);
        return NoContent();
    }
}
