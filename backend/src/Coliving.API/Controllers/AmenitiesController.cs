using Coliving.Application.DTOs;
using Coliving.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coliving.API.Controllers;

/// <summary>Tiện ích chung (bể bơi, gym, BBQ...) và đặt lịch sử dụng.</summary>
[Authorize]
public class AmenitiesController : BaseApiController
{
    private readonly IAmenityService _service;
    private readonly IAmenityBookingService _bookings;
    public AmenitiesController(IAmenityService service, IAmenityBookingService bookings)
    {
        _service = service;
        _bookings = bookings;
    }

    [HttpGet]
    public async Task<ActionResult<List<AmenityDto>>> GetAll([FromQuery] int? buildingId)
        => Ok(await _service.GetAllAsync(buildingId));

    [Authorize(Roles = "Manager,Admin")]
    [HttpPost]
    public async Task<ActionResult<AmenityDto>> Create(SaveAmenityDto dto) => Ok(await _service.CreateAsync(dto));

    [Authorize(Roles = "Manager,Admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<AmenityDto>> Update(int id, SaveAmenityDto dto)
        => Ok(await _service.UpdateAsync(id, dto));

    [Authorize(Roles = "Manager,Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    // ---- Đặt lịch tiện ích ----

    [HttpGet("{id:int}/bookings")]
    public async Task<ActionResult<List<AmenityBookingDto>>> Schedule(int id, [FromQuery] DateTime? day)
        => Ok(await _bookings.GetByAmenityAsync(id, day ?? DateTime.UtcNow));

    [HttpGet("bookings/mine")]
    public async Task<ActionResult<List<AmenityBookingDto>>> MyBookings()
        => Ok(await _bookings.GetMineAsync(CurrentUserId));

    [HttpPost("bookings")]
    public async Task<ActionResult<AmenityBookingDto>> Book(CreateAmenityBookingDto dto)
        => Ok(await _bookings.BookAsync(CurrentUserId, dto));

    [HttpDelete("bookings/{bookingId:int}")]
    public async Task<IActionResult> CancelBooking(int bookingId)
    {
        await _bookings.CancelAsync(CurrentUserId, CurrentRole, bookingId);
        return NoContent();
    }
}
