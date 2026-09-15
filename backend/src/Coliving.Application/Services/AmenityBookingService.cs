using Coliving.Application.DTOs;
using Coliving.Application.Common;
using Coliving.Application.Interfaces;
using Coliving.Domain.Entities;
using Coliving.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Coliving.Application.Services;

public class AmenityBookingService : IAmenityBookingService
{
    private readonly IAppDbContext _db;
    private readonly INotificationService _notify;
    public AmenityBookingService(IAppDbContext db, INotificationService notify)
    {
        _db = db;
        _notify = notify;
    }

    public async Task<List<AmenityBookingDto>> GetByAmenityAsync(int amenityId, DateTime day)
    {
        var start = day.Date;
        var end = start.AddDays(1);
        return await _db.AmenityBookings.AsNoTracking()
            .Include(b => b.Amenity).Include(b => b.User)
            .Where(b => b.AmenityId == amenityId && b.Status != AmenityBookingStatus.Cancelled
                        && b.StartTime >= start && b.StartTime < end)
            .OrderBy(b => b.StartTime)
            .Select(b => ToDto(b))
            .ToListAsync();
    }

    public async Task<List<AmenityBookingDto>> GetMineAsync(int userId)
    {
        return await _db.AmenityBookings.AsNoTracking()
            .Include(b => b.Amenity).Include(b => b.User)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.StartTime)
            .Select(b => ToDto(b))
            .ToListAsync();
    }

    public async Task<AmenityBookingDto> BookAsync(int userId, CreateAmenityBookingDto dto)
    {
        var amenity = await _db.Amenities.FirstOrDefaultAsync(a => a.Id == dto.AmenityId && a.IsActive)
            ?? throw AppException.NotFound("Không tìm thấy tiện ích hoặc tiện ích đang tạm ngưng.");

        if (dto.EndTime <= dto.StartTime)
            throw new AppException("Thời gian kết thúc phải sau thời gian bắt đầu.");
        if (dto.StartTime < DateTime.UtcNow)
            throw new AppException("Không thể đặt lịch trong quá khứ.");
        if (dto.StartTime.Hour < amenity.OpenHour || dto.EndTime.Hour > amenity.CloseHour)
            throw new AppException($"Chỉ được đặt trong giờ mở cửa ({amenity.OpenHour}h–{amenity.CloseHour}h).");

        // Kiểm tra sức chứa: đếm lượt đặt chồng khung giờ.
        var overlapping = await _db.AmenityBookings.CountAsync(b =>
            b.AmenityId == amenity.Id && b.Status == AmenityBookingStatus.Booked &&
            b.StartTime < dto.EndTime && b.EndTime > dto.StartTime);
        if (overlapping >= amenity.Capacity)
            throw AppException.Conflict("Khung giờ này đã đầy chỗ, vui lòng chọn giờ khác.");

        // Tính phí theo số slot.
        var minutes = (dto.EndTime - dto.StartTime).TotalMinutes;
        var slots = (int)Math.Ceiling(minutes / amenity.SlotMinutes);
        var fee = amenity.FeePerSlot * slots;

        var booking = new AmenityBooking
        {
            AmenityId = amenity.Id, UserId = userId,
            StartTime = dto.StartTime, EndTime = dto.EndTime,
            PartySize = Math.Max(1, dto.PartySize), Fee = fee,
            Status = AmenityBookingStatus.Booked, Note = dto.Note
        };
        _db.AmenityBookings.Add(booking);
        await _db.SaveChangesAsync();

        await _notify.NotifyUserAsync(userId, "Đặt tiện ích thành công",
            $"Bạn đã đặt {amenity.Name} lúc {dto.StartTime:HH:mm dd/MM}.", "amenity");

        return await LoadDto(booking.Id);
    }

    public async Task CancelAsync(int userId, string? role, int id)
    {
        var b = await _db.AmenityBookings.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy lượt đặt.");
        var isManager = role is "Manager" or "Admin" or "Staff";
        if (b.UserId != userId && !isManager)
            throw AppException.Forbidden("Bạn không có quyền huỷ lượt đặt này.");
        if (b.Status != AmenityBookingStatus.Booked)
            throw new AppException("Lượt đặt không ở trạng thái có thể huỷ.");
        b.Status = AmenityBookingStatus.Cancelled;
        await _db.SaveChangesAsync();
    }

    private async Task<AmenityBookingDto> LoadDto(int id)
    {
        var b = await _db.AmenityBookings.AsNoTracking()
            .Include(x => x.Amenity).Include(x => x.User).FirstAsync(x => x.Id == id);
        return ToDto(b);
    }

    private static AmenityBookingDto ToDto(AmenityBooking b) => new()
    {
        Id = b.Id, AmenityId = b.AmenityId, AmenityName = b.Amenity?.Name ?? "",
        UserId = b.UserId, UserName = b.User?.FullName ?? "",
        StartTime = b.StartTime, EndTime = b.EndTime, PartySize = b.PartySize,
        Fee = b.Fee, Status = b.Status.ToString(), Note = b.Note
    };
}
