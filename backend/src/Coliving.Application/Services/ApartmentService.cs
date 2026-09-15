using Coliving.Application.DTOs;
using Coliving.Application.Common;
using Coliving.Application.Interfaces;
using Coliving.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Coliving.Application.Services;

public class ApartmentService : IApartmentService
{
    private readonly IAppDbContext _db;
    public ApartmentService(IAppDbContext db) => _db = db;

    public async Task<List<ApartmentDto>> GetAllAsync(int? buildingId)
    {
        var q = _db.Apartments.AsNoTracking().Include(a => a.Building).Include(a => a.Rooms).AsQueryable();
        if (buildingId.HasValue) q = q.Where(a => a.BuildingId == buildingId.Value);
        return await q.OrderBy(a => a.Code).Select(a => ToDto(a)).ToListAsync();
    }

    public async Task<ApartmentDto> GetByIdAsync(int id)
    {
        var a = await _db.Apartments.AsNoTracking().Include(x => x.Building).Include(x => x.Rooms)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy căn hộ.");
        return ToDto(a);
    }

    public async Task<ApartmentDto> CreateAsync(SaveApartmentDto dto)
    {
        if (!await _db.Buildings.AnyAsync(b => b.Id == dto.BuildingId))
            throw AppException.NotFound("Không tìm thấy toà nhà.");
        if (await _db.Apartments.AnyAsync(a => a.BuildingId == dto.BuildingId && a.Code == dto.Code))
            throw AppException.Conflict("Mã căn hộ đã tồn tại trong toà nhà.");

        var a = new Apartment
        {
            BuildingId = dto.BuildingId, Code = dto.Code.Trim(), Floor = dto.Floor,
            Area = dto.Area, BedroomCount = dto.BedroomCount, Description = dto.Description,
            IsActive = dto.IsActive,
            Direction = dto.Direction, Furnishing = dto.Furnishing, HasBalcony = dto.HasBalcony,
            MaintenanceFee = dto.MaintenanceFee, Notes = dto.Notes
        };
        _db.Apartments.Add(a);
        await _db.SaveChangesAsync();
        return await GetByIdAsync(a.Id);
    }

    public async Task<ApartmentDto> UpdateAsync(int id, SaveApartmentDto dto)
    {
        var a = await _db.Apartments.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy căn hộ.");
        a.BuildingId = dto.BuildingId; a.Code = dto.Code.Trim(); a.Floor = dto.Floor;
        a.Area = dto.Area; a.BedroomCount = dto.BedroomCount; a.Description = dto.Description;
        a.IsActive = dto.IsActive;
        a.Direction = dto.Direction; a.Furnishing = dto.Furnishing; a.HasBalcony = dto.HasBalcony;
        a.MaintenanceFee = dto.MaintenanceFee; a.Notes = dto.Notes;
        await _db.SaveChangesAsync();
        return await GetByIdAsync(a.Id);
    }

    public async Task DeleteAsync(int id)
    {
        var a = await _db.Apartments.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy căn hộ.");
        if (await _db.Rooms.AnyAsync(r => r.ApartmentId == id))
            throw AppException.Conflict("Căn hộ còn phòng, không thể xoá.");
        a.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private static ApartmentDto ToDto(Apartment a) => new()
    {
        Id = a.Id, BuildingId = a.BuildingId, BuildingName = a.Building?.Name ?? "",
        Code = a.Code, Floor = a.Floor, Area = a.Area, BedroomCount = a.BedroomCount,
        Description = a.Description, IsActive = a.IsActive,
        Direction = a.Direction, Furnishing = a.Furnishing, HasBalcony = a.HasBalcony,
        MaintenanceFee = a.MaintenanceFee, Notes = a.Notes,
        RoomCount = a.Rooms?.Count ?? 0
    };
}
