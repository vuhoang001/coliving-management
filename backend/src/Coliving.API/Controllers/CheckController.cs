using Coliving.Application.DTOs;
using Coliving.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coliving.API.Controllers;

/// <summary>Nghiệp vụ nhận phòng / trả phòng (check-in / check-out).</summary>
[Authorize(Roles = "Manager,Admin,Staff")]
[Route("api/check")]
public class CheckController : BaseApiController
{
    private readonly ICheckRecordService _service;
    public CheckController(ICheckRecordService service) => _service = service;

    [HttpGet("booking/{bookingId:int}")]
    public async Task<ActionResult<List<CheckRecordDto>>> ByBooking(int bookingId)
        => Ok(await _service.GetByBookingAsync(bookingId));

    [HttpPost("in")]
    public async Task<ActionResult<CheckRecordDto>> CheckIn(CheckActionDto dto)
        => Ok(await _service.CheckInAsync(CurrentUserId, dto));

    [HttpPost("out")]
    public async Task<ActionResult<CheckRecordDto>> CheckOut(CheckActionDto dto)
        => Ok(await _service.CheckOutAsync(CurrentUserId, dto));
}
