using Coliving.Application.Common;
using Coliving.Application.DTOs;
using Coliving.Application.Interfaces;
using Coliving.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Coliving.Application.Services;

public class BuildingService : IBuildingService
{
    private readonly IAppDbContext _db;
    public BuildingService(IAppDbContext db) => _db = db;

    public async Task<List<BuildingDto>> GetAllAsync()
    {
        return await _db.Buildings.AsNoTracking()
            .OrderBy(b => b.Name)
            .Select(b => new BuildingDto
            {
                Id = b.Id, Name = b.Name, Address = b.Address, City = b.City,
                Description = b.Description, ImageUrl = b.ImageUrl, Floors = b.Floors,
                IsActive = b.IsActive,
                District = b.District, Ward = b.Ward, ContactPhone = b.ContactPhone,
                ContactEmail = b.ContactEmail, YearBuilt = b.YearBuilt,
                TotalFloorArea = b.TotalFloorArea, ParkingSlots = b.ParkingSlots,
                HasElevator = b.HasElevator, Notes = b.Notes,
                ApartmentCount = b.Apartments.Count,
                RoomCount = b.Apartments.SelectMany(a => a.Rooms).Count()
            })
            .ToListAsync();
    }

    public async Task<BuildingDto> GetByIdAsync(int id)
    {
        var b = await _db.Buildings.AsNoTracking()
            .Include(x => x.Apartments).ThenInclude(a => a.Rooms)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy toà nhà.");
        return ToDto(b);
    }

    public async Task<BuildingDto> CreateAsync(SaveBuildingDto dto)
    {
        var b = new Building
        {
            Name = dto.Name.Trim(), Address = dto.Address.Trim(), City = dto.City.Trim(),
            Description = dto.Description, ImageUrl = dto.ImageUrl,
            Floors = dto.Floors, IsActive = dto.IsActive,
            District = dto.District.Trim(), Ward = dto.Ward, ContactPhone = dto.ContactPhone,
            ContactEmail = dto.ContactEmail, YearBuilt = dto.YearBuilt,
            TotalFloorArea = dto.TotalFloorArea, ParkingSlots = dto.ParkingSlots,
            HasElevator = dto.HasElevator, Notes = dto.Notes
        };
        _db.Buildings.Add(b);
        await _db.SaveChangesAsync();
        return ToDto(b);
    }

    public async Task<BuildingDto> UpdateAsync(int id, SaveBuildingDto dto)
    {
        var b = await _db.Buildings.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy toà nhà.");
        b.Name = dto.Name.Trim(); b.Address = dto.Address.Trim(); b.City = dto.City.Trim();
        b.Description = dto.Description; b.ImageUrl = dto.ImageUrl;
        b.Floors = dto.Floors; b.IsActive = dto.IsActive;
        b.District = dto.District.Trim(); b.Ward = dto.Ward; b.ContactPhone = dto.ContactPhone;
        b.ContactEmail = dto.ContactEmail; b.YearBuilt = dto.YearBuilt;
        b.TotalFloorArea = dto.TotalFloorArea; b.ParkingSlots = dto.ParkingSlots;
        b.HasElevator = dto.HasElevator; b.Notes = dto.Notes;
        await _db.SaveChangesAsync();
        return ToDto(b);
    }

    public async Task DeleteAsync(int id)
    {
        var b = await _db.Buildings.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy toà nhà.");
        if (await _db.Apartments.AnyAsync(a => a.BuildingId == id))
            throw AppException.Conflict("Toà nhà còn căn hộ, không thể xoá.");
        b.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private static BuildingDto ToDto(Building b) => new()
    {
        Id = b.Id, Name = b.Name, Address = b.Address, City = b.City,
        Description = b.Description, ImageUrl = b.ImageUrl, Floors = b.Floors, IsActive = b.IsActive,
        District = b.District, Ward = b.Ward, ContactPhone = b.ContactPhone,
        ContactEmail = b.ContactEmail, YearBuilt = b.YearBuilt, TotalFloorArea = b.TotalFloorArea,
        ParkingSlots = b.ParkingSlots, HasElevator = b.HasElevator, Notes = b.Notes,
        ApartmentCount = b.Apartments?.Count ?? 0,
        RoomCount = b.Apartments?.SelectMany(a => a.Rooms).Count() ?? 0
    };
}
