using Coliving.Application.DTOs;
using Coliving.Application.Common;
using Coliving.Application.Interfaces;
using Coliving.Domain.Entities;
using Coliving.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Coliving.Application.Services;

/// <summary>Nghiệp vụ nhận phòng / trả phòng (check-in / check-out).</summary>
public class CheckRecordService : ICheckRecordService
{
    private readonly IAppDbContext _db;
    public CheckRecordService(IAppDbContext db) => _db = db;

    public async Task<List<CheckRecordDto>> GetByBookingAsync(int bookingId)
        => await _db.CheckRecords.AsNoTracking()
            .Include(c => c.Booking)
            .Where(c => c.BookingId == bookingId)
            .OrderBy(c => c.RecordedAt)
            .Select(c => ToDto(c))
            .ToListAsync();

    public async Task<CheckRecordDto> CheckInAsync(int staffId, CheckActionDto dto)
    {
        var booking = await _db.Bookings.Include(b => b.Room)
            .FirstOrDefaultAsync(b => b.Id == dto.BookingId)
            ?? throw AppException.NotFound("Không tìm thấy đặt phòng.");
        if (booking.Status != BookingStatus.Confirmed)
            throw new AppException("Chỉ đặt phòng đã xác nhận mới có thể nhận phòng.");

        booking.Status = BookingStatus.CheckedIn;
        booking.Room.Status = RoomStatus.Occupied;

        var record = Create(dto, staffId, CheckRecordType.CheckIn);
        _db.CheckRecords.Add(record);
        await _db.SaveChangesAsync();
        return await LoadDto(record.Id);
    }

    public async Task<CheckRecordDto> CheckOutAsync(int staffId, CheckActionDto dto)
    {
        var booking = await _db.Bookings.Include(b => b.Room)
            .FirstOrDefaultAsync(b => b.Id == dto.BookingId)
            ?? throw AppException.NotFound("Không tìm thấy đặt phòng.");
        if (booking.Status != BookingStatus.CheckedIn)
            throw new AppException("Chỉ đặt phòng đang ở mới có thể trả phòng.");

        booking.Status = BookingStatus.CheckedOut;

        // Chỉ trả phòng về trống khi không còn ai đang ở (ở ghép).
        var stillOccupied = await _db.Bookings.AnyAsync(b => b.RoomId == booking.RoomId
            && b.Id != booking.Id && b.Status == BookingStatus.CheckedIn);
        if (!stillOccupied && booking.Room.Status != RoomStatus.Maintenance)
            booking.Room.Status = RoomStatus.Available;

        var record = Create(dto, staffId, CheckRecordType.CheckOut);
        _db.CheckRecords.Add(record);
        await _db.SaveChangesAsync();
        return await LoadDto(record.Id);
    }

    private static CheckRecord Create(CheckActionDto dto, int staffId, CheckRecordType type) => new()
    {
        BookingId = dto.BookingId, Type = type, StaffId = staffId,
        RecordedAt = DateTime.UtcNow,
        ElectricityMeter = dto.ElectricityMeter, WaterMeter = dto.WaterMeter,
        ConditionNote = dto.ConditionNote, PhotoUrl = dto.PhotoUrl
    };

    private async Task<CheckRecordDto> LoadDto(int id)
    {
        var c = await _db.CheckRecords.AsNoTracking().Include(x => x.Booking).FirstAsync(x => x.Id == id);
        return ToDto(c);
    }

    private static CheckRecordDto ToDto(CheckRecord c) => new()
    {
        Id = c.Id, BookingId = c.BookingId, BookingCode = c.Booking?.Code ?? "",
        Type = c.Type.ToString(), RecordedAt = c.RecordedAt, StaffId = c.StaffId,
        ElectricityMeter = c.ElectricityMeter, WaterMeter = c.WaterMeter,
        ConditionNote = c.ConditionNote, PhotoUrl = c.PhotoUrl
    };
}
