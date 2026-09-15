using Coliving.Application.Common;
using Coliving.Application.DTOs;
using Coliving.Application.Interfaces;
using Coliving.Domain.Entities;
using Coliving.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Coliving.Application.Services;

public class RoomService : IRoomService
{
    private readonly IAppDbContext _db;
    public RoomService(IAppDbContext db) => _db = db;

    private IQueryable<Room> BaseQuery() =>
        _db.Rooms.AsNoTracking()
            .Include(r => r.Apartment).ThenInclude(a => a.Building);

    public async Task<PagedResult<RoomDto>> SearchAsync(RoomFilterDto filter)
    {
        var q = BaseQuery();

        if (filter.BuildingId.HasValue) q = q.Where(r => r.Apartment.BuildingId == filter.BuildingId.Value);
        if (filter.ApartmentId.HasValue) q = q.Where(r => r.ApartmentId == filter.ApartmentId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Type) && Enum.TryParse<RoomType>(filter.Type, true, out var t))
            q = q.Where(r => r.Type == t);
        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<RoomStatus>(filter.Status, true, out var s))
            q = q.Where(r => r.Status == s);
        if (filter.MinPrice.HasValue) q = q.Where(r => r.MonthlyPrice >= filter.MinPrice.Value);
        if (filter.MaxPrice.HasValue) q = q.Where(r => r.MonthlyPrice <= filter.MaxPrice.Value);
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var k = filter.Keyword.Trim().ToLower();
            q = q.Where(r => r.Code.ToLower().Contains(k) || r.Apartment.Code.ToLower().Contains(k));
        }

        var total = await q.CountAsync();
        var rooms = await q.OrderBy(r => r.Code)
            .Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize)
            .ToListAsync();

        var items = new List<RoomDto>();
        foreach (var r in rooms) items.Add(await ToDtoAsync(r));

        return new PagedResult<RoomDto>
        {
            Items = items, Page = filter.Page, PageSize = filter.PageSize, TotalItems = total
        };
    }

    public async Task<List<RoomDto>> GetAvailableAsync(DateTime? from, DateTime? to)
    {
        var rooms = await BaseQuery()
            .Where(r => r.Status == RoomStatus.Available || r.Status == RoomStatus.Occupied)
            .OrderBy(r => r.Code)
            .ToListAsync();

        var result = new List<RoomDto>();
        foreach (var r in rooms)
        {
            var occupants = await CountOccupantsAsync(r.Id, from, to);
            if (occupants < r.Capacity && r.Status != RoomStatus.Maintenance)
                result.Add(await ToDtoAsync(r, occupants));
        }
        return result;
    }

    public async Task<RoomDto> GetByIdAsync(int id)
    {
        var r = await BaseQuery().FirstOrDefaultAsync(x => x.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy phòng.");
        return await ToDtoAsync(r);
    }

    public async Task<RoomDto> CreateAsync(SaveRoomDto dto)
    {
        if (!await _db.Apartments.AnyAsync(a => a.Id == dto.ApartmentId))
            throw AppException.NotFound("Không tìm thấy căn hộ.");
        if (await _db.Rooms.AnyAsync(r => r.Code == dto.Code))
            throw AppException.Conflict("Mã phòng đã tồn tại.");
        if (!Enum.TryParse<RoomType>(dto.Type, true, out var type))
            throw new AppException("Loại phòng không hợp lệ.");

        var r = new Room
        {
            ApartmentId = dto.ApartmentId, Code = dto.Code.Trim(), Type = type,
            Capacity = Math.Max(1, dto.Capacity), Area = dto.Area,
            MonthlyPrice = dto.MonthlyPrice, Deposit = dto.Deposit, Description = dto.Description,
            HasWindow = dto.HasWindow, HasPrivateBathroom = dto.HasPrivateBathroom,
            HasAirConditioner = dto.HasAirConditioner, ElectricityUnitPrice = dto.ElectricityUnitPrice,
            WaterUnitPrice = dto.WaterUnitPrice, InternetFee = dto.InternetFee,
            ImageUrl = dto.ImageUrl, Notes = dto.Notes
        };
        _db.Rooms.Add(r);
        await _db.SaveChangesAsync();
        return await GetByIdAsync(r.Id);
    }

    public async Task<RoomDto> UpdateAsync(int id, SaveRoomDto dto)
    {
        var r = await _db.Rooms.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy phòng.");
        if (!Enum.TryParse<RoomType>(dto.Type, true, out var type))
            throw new AppException("Loại phòng không hợp lệ.");

        r.ApartmentId = dto.ApartmentId; r.Code = dto.Code.Trim(); r.Type = type;
        r.Capacity = Math.Max(1, dto.Capacity); r.Area = dto.Area;
        r.MonthlyPrice = dto.MonthlyPrice; r.Deposit = dto.Deposit; r.Description = dto.Description;
        r.HasWindow = dto.HasWindow; r.HasPrivateBathroom = dto.HasPrivateBathroom;
        r.HasAirConditioner = dto.HasAirConditioner; r.ElectricityUnitPrice = dto.ElectricityUnitPrice;
        r.WaterUnitPrice = dto.WaterUnitPrice; r.InternetFee = dto.InternetFee;
        r.ImageUrl = dto.ImageUrl; r.Notes = dto.Notes;
        await _db.SaveChangesAsync();
        return await GetByIdAsync(r.Id);
    }

    public async Task<RoomDto> SetStatusAsync(int id, string status)
    {
        var r = await _db.Rooms.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy phòng.");
        if (!Enum.TryParse<RoomStatus>(status, true, out var s))
            throw new AppException("Trạng thái phòng không hợp lệ.");
        r.Status = s;
        await _db.SaveChangesAsync();
        return await GetByIdAsync(r.Id);
    }

    public async Task DeleteAsync(int id)
    {
        var r = await _db.Rooms.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy phòng.");
        var hasActive = await _db.Bookings.AnyAsync(b => b.RoomId == id &&
            (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn));
        if (hasActive) throw AppException.Conflict("Phòng còn hợp đồng/đặt phòng đang hiệu lực.");
        r.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    /// <summary>Đếm số người đang chiếm chỗ phòng (CheckedIn, hoặc Confirmed trùng khoảng thời gian).</summary>
    private async Task<int> CountOccupantsAsync(int roomId, DateTime? from = null, DateTime? to = null)
    {
        var q = _db.Bookings.Where(b => b.RoomId == roomId &&
            (b.Status == BookingStatus.CheckedIn || b.Status == BookingStatus.Confirmed));
        if (from.HasValue && to.HasValue)
            q = q.Where(b => b.CheckInDate < to.Value && b.CheckOutDate > from.Value);
        return await q.CountAsync();
    }

    private async Task<RoomDto> ToDtoAsync(Room r, int? occupants = null)
    {
        occupants ??= await CountOccupantsAsync(r.Id);
        return new RoomDto
        {
            Id = r.Id, ApartmentId = r.ApartmentId,
            ApartmentCode = r.Apartment?.Code ?? "",
            BuildingName = r.Apartment?.Building?.Name ?? "",
            Code = r.Code, Type = r.Type.ToString(), Capacity = r.Capacity,
            CurrentOccupants = occupants.Value, Area = r.Area,
            MonthlyPrice = r.MonthlyPrice, Deposit = r.Deposit,
            Status = r.Status.ToString(), Description = r.Description,
            HasWindow = r.HasWindow, HasPrivateBathroom = r.HasPrivateBathroom,
            HasAirConditioner = r.HasAirConditioner, ElectricityUnitPrice = r.ElectricityUnitPrice,
            WaterUnitPrice = r.WaterUnitPrice, InternetFee = r.InternetFee,
            ImageUrl = r.ImageUrl, Notes = r.Notes
        };
    }
}
