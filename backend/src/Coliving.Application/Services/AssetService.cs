using Coliving.Application.Common;
using Coliving.Application.DTOs;
using Coliving.Application.Interfaces;
using Coliving.Domain.Entities;
using Coliving.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Coliving.Application.Services;

public class AssetService : IAssetService
{
    private readonly IAppDbContext _db;
    public AssetService(IAppDbContext db) => _db = db;

    public async Task<PagedResult<AssetDto>> SearchAsync(AssetFilterDto filter)
    {
        var q = _db.Assets.AsNoTracking()
            .Include(a => a.Building).Include(a => a.Apartment).Include(a => a.Room)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Category)) q = q.Where(a => a.Category == filter.Category);
        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<AssetStatus>(filter.Status, true, out var s))
            q = q.Where(a => a.Status == s);
        if (filter.BuildingId.HasValue) q = q.Where(a => a.BuildingId == filter.BuildingId.Value);
        if (filter.RoomId.HasValue) q = q.Where(a => a.RoomId == filter.RoomId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var k = filter.Keyword.Trim().ToLower();
            q = q.Where(a => a.Name.ToLower().Contains(k) ||
                (a.SerialNumber != null && a.SerialNumber.ToLower().Contains(k)));
        }

        var total = await q.CountAsync();
        var items = await q.OrderBy(a => a.Name)
            .Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize)
            .Select(a => ToDto(a))
            .ToListAsync();

        return new PagedResult<AssetDto>
        {
            Items = items, Page = filter.Page, PageSize = filter.PageSize, TotalItems = total
        };
    }

    public async Task<AssetDto> GetByIdAsync(int id)
    {
        var a = await _db.Assets.AsNoTracking()
            .Include(x => x.Building).Include(x => x.Apartment).Include(x => x.Room)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy tài sản.");
        return ToDto(a);
    }

    public async Task<AssetDto> CreateAsync(SaveAssetDto dto)
    {
        var a = new Asset();
        Apply(a, dto);
        _db.Assets.Add(a);
        await _db.SaveChangesAsync();
        return await GetByIdAsync(a.Id);
    }

    public async Task<AssetDto> UpdateAsync(int id, SaveAssetDto dto)
    {
        var a = await _db.Assets.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy tài sản.");
        Apply(a, dto);
        await _db.SaveChangesAsync();
        return await GetByIdAsync(a.Id);
    }

    public async Task DeleteAsync(int id)
    {
        var a = await _db.Assets.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy tài sản.");
        a.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private static void Apply(Asset a, SaveAssetDto dto)
    {
        if (!Enum.TryParse<AssetStatus>(dto.Status, true, out var status))
            throw new AppException("Trạng thái tài sản không hợp lệ.");
        a.Name = dto.Name.Trim(); a.Category = dto.Category; a.SerialNumber = dto.SerialNumber;
        a.Status = status; a.PurchaseValue = dto.PurchaseValue; a.PurchaseDate = dto.PurchaseDate;
        a.LastMaintenanceAt = dto.LastMaintenanceAt; a.Note = dto.Note;
        a.Brand = dto.Brand; a.Model = dto.Model; a.Quantity = Math.Max(1, dto.Quantity);
        a.WarrantyUntil = dto.WarrantyUntil; a.Supplier = dto.Supplier;
        a.BuildingId = dto.BuildingId; a.ApartmentId = dto.ApartmentId; a.RoomId = dto.RoomId;
    }

    private static AssetDto ToDto(Asset a) => new()
    {
        Id = a.Id, Name = a.Name, Category = a.Category, SerialNumber = a.SerialNumber,
        Status = a.Status.ToString(), PurchaseValue = a.PurchaseValue, PurchaseDate = a.PurchaseDate,
        LastMaintenanceAt = a.LastMaintenanceAt, Note = a.Note,
        Brand = a.Brand, Model = a.Model, Quantity = a.Quantity,
        WarrantyUntil = a.WarrantyUntil, Supplier = a.Supplier,
        BuildingId = a.BuildingId, ApartmentId = a.ApartmentId, RoomId = a.RoomId,
        LocationLabel = a.Room != null ? $"Phòng {a.Room.Code}"
            : a.Apartment != null ? $"Căn hộ {a.Apartment.Code}"
            : a.Building != null ? a.Building.Name : "Kho / chung"
    };
}
