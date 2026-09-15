using Coliving.Application.DTOs;
using Coliving.Application.Common;
using Coliving.Application.Interfaces;
using Coliving.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Coliving.Application.Services;

public class AmenityService : IAmenityService
{
    private readonly IAppDbContext _db;
    public AmenityService(IAppDbContext db) => _db = db;

    public async Task<List<AmenityDto>> GetAllAsync(int? buildingId)
    {
        var q = _db.Amenities.AsNoTracking().Include(a => a.Building).AsQueryable();
        if (buildingId.HasValue) q = q.Where(a => a.BuildingId == buildingId.Value);
        return await q.OrderBy(a => a.Name).Select(a => ToDto(a)).ToListAsync();
    }

    public async Task<AmenityDto> CreateAsync(SaveAmenityDto dto)
    {
        if (!await _db.Buildings.AnyAsync(b => b.Id == dto.BuildingId))
            throw AppException.NotFound("Không tìm thấy toà nhà.");
        var a = new Amenity { BuildingId = dto.BuildingId };
        Apply(a, dto);
        _db.Amenities.Add(a);
        await _db.SaveChangesAsync();
        return await LoadDto(a.Id);
    }

    public async Task<AmenityDto> UpdateAsync(int id, SaveAmenityDto dto)
    {
        var a = await _db.Amenities.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy tiện ích.");
        a.BuildingId = dto.BuildingId;
        Apply(a, dto);
        await _db.SaveChangesAsync();
        return await LoadDto(a.Id);
    }

    public async Task DeleteAsync(int id)
    {
        var a = await _db.Amenities.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy tiện ích.");
        a.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private async Task<AmenityDto> LoadDto(int id)
    {
        var a = await _db.Amenities.AsNoTracking().Include(x => x.Building).FirstAsync(x => x.Id == id);
        return ToDto(a);
    }

    private static void Apply(Amenity a, SaveAmenityDto dto)
    {
        a.Name = dto.Name.Trim(); a.Description = dto.Description; a.Capacity = Math.Max(1, dto.Capacity);
        a.OpenHour = dto.OpenHour; a.CloseHour = dto.CloseHour; a.SlotMinutes = Math.Max(15, dto.SlotMinutes);
        a.FeePerSlot = dto.FeePerSlot; a.IsActive = dto.IsActive;
    }

    private static AmenityDto ToDto(Amenity a) => new()
    {
        Id = a.Id, BuildingId = a.BuildingId, BuildingName = a.Building?.Name ?? "",
        Name = a.Name, Description = a.Description, Capacity = a.Capacity,
        OpenHour = a.OpenHour, CloseHour = a.CloseHour, SlotMinutes = a.SlotMinutes,
        FeePerSlot = a.FeePerSlot, IsActive = a.IsActive
    };
}
