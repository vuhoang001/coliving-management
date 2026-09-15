using Coliving.Application.DTOs;
using Coliving.Application.Common;
using Coliving.Application.Interfaces;
using Coliving.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Coliving.Application.Services;

public class ServiceCatalogService : IServiceCatalogService
{
    private readonly IAppDbContext _db;
    public ServiceCatalogService(IAppDbContext db) => _db = db;

    public async Task<List<ServiceCatalogDto>> GetAllAsync(bool onlyActive)
    {
        var q = _db.ServiceCatalogs.AsNoTracking().AsQueryable();
        if (onlyActive) q = q.Where(s => s.IsActive);
        return await q.OrderBy(s => s.Category).ThenBy(s => s.Name)
            .Select(s => ToDto(s)).ToListAsync();
    }

    public async Task<ServiceCatalogDto> CreateAsync(SaveServiceCatalogDto dto)
    {
        var s = new ServiceCatalog();
        Apply(s, dto);
        _db.ServiceCatalogs.Add(s);
        await _db.SaveChangesAsync();
        return ToDto(s);
    }

    public async Task<ServiceCatalogDto> UpdateAsync(int id, SaveServiceCatalogDto dto)
    {
        var s = await _db.ServiceCatalogs.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy dịch vụ.");
        Apply(s, dto);
        await _db.SaveChangesAsync();
        return ToDto(s);
    }

    public async Task DeleteAsync(int id)
    {
        var s = await _db.ServiceCatalogs.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy dịch vụ.");
        s.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private static void Apply(ServiceCatalog s, SaveServiceCatalogDto dto)
    {
        s.Name = dto.Name.Trim(); s.Description = dto.Description; s.Category = dto.Category;
        s.UnitPrice = dto.UnitPrice; s.Unit = dto.Unit; s.IsActive = dto.IsActive;
    }

    private static ServiceCatalogDto ToDto(ServiceCatalog s) => new()
    {
        Id = s.Id, Name = s.Name, Description = s.Description, Category = s.Category,
        UnitPrice = s.UnitPrice, Unit = s.Unit, IsActive = s.IsActive
    };
}
