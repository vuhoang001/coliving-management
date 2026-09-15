using Coliving.Application.Common;
using Coliving.Application.DTOs;
using Coliving.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coliving.API.Controllers;

[Authorize]
public class BookingsController : BaseApiController
{
    private readonly IBookingService _service;
    public BookingsController(IBookingService service) => _service = service;

    [Authorize(Roles = "Manager,Admin,Staff")]
    [HttpGet]
    public async Task<ActionResult<PagedResult<BookingDto>>> GetAll([FromQuery] PaginationQuery query,
        [FromQuery] string? status)
        => Ok(await _service.GetAllAsync(query, status));

    [HttpGet("mine")]
    public async Task<ActionResult<List<BookingDto>>> Mine() => Ok(await _service.GetMineAsync(CurrentUserId));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookingDto>> Get(int id) => Ok(await _service.GetByIdAsync(id));

    [HttpPost]
    public async Task<ActionResult<BookingDto>> Create(CreateBookingDto dto)
        => Ok(await _service.CreateAsync(CurrentUserId, CurrentRole, dto));

    [Authorize(Roles = "Manager,Admin,Staff")]
    [HttpPut("{id:int}/confirm")]
    public async Task<ActionResult<BookingDto>> Confirm(int id) => Ok(await _service.ConfirmAsync(id));

    [HttpPut("{id:int}/cancel")]
    public async Task<ActionResult<BookingDto>> Cancel(int id, [FromQuery] string? reason)
        => Ok(await _service.CancelAsync(CurrentUserId, CurrentRole, id, reason));
}
