using Coliving.Application.Common;
using Coliving.Application.DTOs;
using Coliving.Application.Interfaces;
using Coliving.Domain.Entities;
using Coliving.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Coliving.Application.Services;

public class BookingService : IBookingService
{
    private readonly IAppDbContext _db;
    private readonly INotificationService _notify;
    public BookingService(IAppDbContext db, INotificationService notify)
    {
        _db = db;
        _notify = notify;
    }

    private IQueryable<Booking> BaseQuery() =>
        _db.Bookings.AsNoTracking()
            .Include(b => b.Tenant)
            .Include(b => b.Room).ThenInclude(r => r.Apartment).ThenInclude(a => a.Building)
            .Include(b => b.Contract);

    public async Task<PagedResult<BookingDto>> GetAllAsync(PaginationQuery query, string? status)
    {
        var q = BaseQuery();
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<BookingStatus>(status, true, out var s))
            q = q.Where(b => b.Status == s);

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(b => b.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(b => ToDto(b))
            .ToListAsync();
        return new PagedResult<BookingDto>
        {
            Items = items, Page = query.Page, PageSize = query.PageSize, TotalItems = total
        };
    }

    public async Task<List<BookingDto>> GetMineAsync(int userId)
        => await BaseQuery().Where(b => b.TenantId == userId)
            .OrderByDescending(b => b.CreatedAt).Select(b => ToDto(b)).ToListAsync();

    public async Task<BookingDto> GetByIdAsync(int id)
    {
        var b = await BaseQuery().FirstOrDefaultAsync(x => x.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy đặt phòng.");
        return ToDto(b);
    }

    public async Task<BookingDto> CreateAsync(int actorId, string? role, CreateBookingDto dto)
    {
        var isManager = role is "Manager" or "Admin" or "Staff";
        var tenantId = dto.TenantId is > 0 && isManager ? dto.TenantId!.Value : actorId;

        if (dto.CheckOutDate <= dto.CheckInDate)
            throw new AppException("Ngày trả phòng phải sau ngày nhận phòng.");

        var room = await _db.Rooms.Include(r => r.Apartment)
            .FirstOrDefaultAsync(r => r.Id == dto.RoomId)
            ?? throw AppException.NotFound("Không tìm thấy phòng.");
        if (room.Status == RoomStatus.Maintenance)
            throw AppException.Conflict("Phòng đang bảo trì, không thể đặt.");

        if (!await _db.Users.AnyAsync(u => u.Id == tenantId))
            throw AppException.NotFound("Không tìm thấy khách thuê.");

        // Kiểm tra sức chứa trong khoảng thời gian (hỗ trợ ở ghép).
        var overlap = await _db.Bookings.CountAsync(b => b.RoomId == room.Id &&
            (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn) &&
            b.CheckInDate < dto.CheckOutDate && b.CheckOutDate > dto.CheckInDate);
        if (overlap >= room.Capacity)
            throw AppException.Conflict("Phòng đã đủ người trong khoảng thời gian này.");

        var booking = new Booking
        {
            Code = CodeGenerator.New("BK"),
            RoomId = room.Id, TenantId = tenantId,
            CheckInDate = dto.CheckInDate, CheckOutDate = dto.CheckOutDate,
            MonthlyPrice = room.MonthlyPrice, Deposit = room.Deposit,
            Status = isManager ? BookingStatus.Confirmed : BookingStatus.Pending,
            Note = dto.Note,
            NumberOfOccupants = Math.Max(1, dto.NumberOfOccupants), Purpose = dto.Purpose,
            SourceChannel = string.IsNullOrWhiteSpace(dto.SourceChannel) ? "Website" : dto.SourceChannel,
            VehiclePlate = dto.VehiclePlate
        };
        _db.Bookings.Add(booking);
        if (booking.Status == BookingStatus.Confirmed && room.Status == RoomStatus.Available)
            room.Status = RoomStatus.Reserved;
        await _db.SaveChangesAsync();

        if (!isManager)
            await _notify.NotifyRoleAsync(UserRole.Manager, "Yêu cầu đặt phòng mới",
                $"Có yêu cầu đặt phòng {room.Code} ({booking.Code}) cần duyệt.", "booking", $"/bookings/{booking.Id}");

        return await GetByIdAsync(booking.Id);
    }

    public async Task<BookingDto> ConfirmAsync(int id)
    {
        var b = await _db.Bookings.Include(x => x.Room).FirstOrDefaultAsync(x => x.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy đặt phòng.");
        if (b.Status != BookingStatus.Pending)
            throw new AppException("Chỉ đặt phòng đang chờ mới có thể xác nhận.");
        b.Status = BookingStatus.Confirmed;
        if (b.Room.Status == RoomStatus.Available) b.Room.Status = RoomStatus.Reserved;
        await _db.SaveChangesAsync();
        await _notify.NotifyUserAsync(b.TenantId, "Đặt phòng được xác nhận",
            $"Đặt phòng {b.Code} đã được xác nhận.", "booking", $"/bookings/{b.Id}");
        return await GetByIdAsync(b.Id);
    }

    public async Task<BookingDto> CancelAsync(int actorId, string? role, int id, string? reason)
    {
        var b = await _db.Bookings.Include(x => x.Room).FirstOrDefaultAsync(x => x.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy đặt phòng.");
        var isManager = role is "Manager" or "Admin" or "Staff";
        if (b.TenantId != actorId && !isManager)
            throw AppException.Forbidden("Bạn không có quyền huỷ đặt phòng này.");
        if (b.Status is BookingStatus.CheckedOut or BookingStatus.Cancelled)
            throw new AppException("Đặt phòng không thể huỷ ở trạng thái hiện tại.");

        b.Status = BookingStatus.Cancelled;
        b.Note = string.IsNullOrWhiteSpace(reason) ? b.Note : $"{b.Note}\n[Huỷ] {reason}";
        // Trả phòng về trạng thái trống nếu không còn ai giữ chỗ.
        var stillActive = await _db.Bookings.AnyAsync(x => x.RoomId == b.RoomId && x.Id != b.Id &&
            (x.Status == BookingStatus.Confirmed || x.Status == BookingStatus.CheckedIn));
        if (!stillActive && b.Room.Status != RoomStatus.Maintenance)
            b.Room.Status = RoomStatus.Available;
        await _db.SaveChangesAsync();
        return await GetByIdAsync(b.Id);
    }

    internal static BookingDto ToDto(Booking b) => new()
    {
        Id = b.Id, Code = b.Code, RoomId = b.RoomId,
        RoomCode = b.Room?.Code ?? "",
        BuildingName = b.Room?.Apartment?.Building?.Name ?? "",
        TenantId = b.TenantId, TenantName = b.Tenant?.FullName ?? "",
        CheckInDate = b.CheckInDate, CheckOutDate = b.CheckOutDate,
        MonthlyPrice = b.MonthlyPrice, Deposit = b.Deposit,
        Status = b.Status.ToString(), Note = b.Note,
        NumberOfOccupants = b.NumberOfOccupants, Purpose = b.Purpose,
        SourceChannel = b.SourceChannel, VehiclePlate = b.VehiclePlate,
        HasContract = b.Contract != null, CreatedAt = b.CreatedAt
    };
}
