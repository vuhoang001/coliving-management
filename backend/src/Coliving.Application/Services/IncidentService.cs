using Coliving.Application.Common;
using Coliving.Application.DTOs;
using Coliving.Application.Interfaces;
using Coliving.Domain.Entities;
using Coliving.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Coliving.Application.Services;

public class IncidentService : IIncidentService
{
    private readonly IAppDbContext _db;
    private readonly INotificationService _notify;
    public IncidentService(IAppDbContext db, INotificationService notify)
    {
        _db = db;
        _notify = notify;
    }

    private IQueryable<Incident> BaseQuery() =>
        _db.Incidents.AsNoTracking().Include(i => i.Reporter).Include(i => i.AssignedTo);

    public async Task<PagedResult<IncidentDto>> GetAllAsync(IncidentFilterDto filter)
    {
        var q = BaseQuery();
        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<IncidentStatus>(filter.Status, true, out var st))
            q = q.Where(i => i.Status == st);
        if (!string.IsNullOrWhiteSpace(filter.Priority) && Enum.TryParse<IncidentPriority>(filter.Priority, true, out var p))
            q = q.Where(i => i.Priority == p);
        if (filter.BuildingId.HasValue) q = q.Where(i => i.BuildingId == filter.BuildingId.Value);
        if (filter.AssignedToId.HasValue) q = q.Where(i => i.AssignedToId == filter.AssignedToId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var k = filter.Keyword.Trim().ToLower();
            q = q.Where(i => i.Title.ToLower().Contains(k) || i.Code.ToLower().Contains(k));
        }

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(i => i.Priority).ThenByDescending(i => i.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize)
            .Select(i => ToDto(i))
            .ToListAsync();
        return new PagedResult<IncidentDto>
        {
            Items = items, Page = filter.Page, PageSize = filter.PageSize, TotalItems = total
        };
    }

    public async Task<List<IncidentDto>> GetMineAsync(int userId)
        => await BaseQuery().Where(i => i.ReporterId == userId)
            .OrderByDescending(i => i.CreatedAt).Select(i => ToDto(i)).ToListAsync();

    public async Task<IncidentDto> GetByIdAsync(int id)
    {
        var i = await BaseQuery().FirstOrDefaultAsync(x => x.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy sự cố.");
        return ToDto(i);
    }

    public async Task<IncidentDto> CreateAsync(int reporterId, CreateIncidentDto dto)
    {
        if (!Enum.TryParse<IncidentPriority>(dto.Priority, true, out var priority))
            priority = IncidentPriority.Medium;

        var incident = new Incident
        {
            Code = CodeGenerator.New("INC"),
            Title = dto.Title.Trim(), Description = dto.Description.Trim(),
            ReporterId = reporterId, Priority = priority, Status = IncidentStatus.Open,
            BuildingId = dto.BuildingId, ApartmentId = dto.ApartmentId,
            RoomId = dto.RoomId, AssetId = dto.AssetId, PhotoUrl = dto.PhotoUrl,
            Category = string.IsNullOrWhiteSpace(dto.Category) ? "Other" : dto.Category,
            LocationDetail = dto.LocationDetail, ContactPhone = dto.ContactPhone,
            ExpectedResolutionDate = dto.ExpectedResolutionDate, Cost = dto.Cost
        };
        _db.Incidents.Add(incident);

        // Nếu sự cố gắn với tài sản → cập nhật tình trạng tài sản cần sửa.
        if (dto.AssetId.HasValue)
        {
            var asset = await _db.Assets.FirstOrDefaultAsync(a => a.Id == dto.AssetId.Value);
            if (asset != null && asset.Status == AssetStatus.Good)
                asset.Status = AssetStatus.NeedsRepair;
        }
        await _db.SaveChangesAsync();

        await _notify.NotifyRoleAsync(UserRole.Staff, "Sự cố mới cần xử lý",
            $"[{priority}] {incident.Title} ({incident.Code})", "incident", $"/incidents/{incident.Id}");

        return await GetByIdAsync(incident.Id);
    }

    public async Task<IncidentDto> AssignAsync(int id, int staffId)
    {
        var i = await _db.Incidents.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy sự cố.");
        if (!await _db.Users.AnyAsync(u => u.Id == staffId))
            throw AppException.NotFound("Không tìm thấy nhân viên.");
        i.AssignedToId = staffId;
        if (i.Status == IncidentStatus.Open) i.Status = IncidentStatus.InProgress;
        await _db.SaveChangesAsync();

        await _notify.NotifyUserAsync(staffId, "Bạn được giao xử lý sự cố",
            $"{i.Title} ({i.Code})", "incident", $"/incidents/{i.Id}");

        return await GetByIdAsync(i.Id);
    }

    public async Task<IncidentDto> UpdateStatusAsync(int id, UpdateIncidentStatusDto dto)
    {
        var i = await _db.Incidents.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy sự cố.");
        if (!Enum.TryParse<IncidentStatus>(dto.Status, true, out var status))
            throw new AppException("Trạng thái sự cố không hợp lệ.");

        i.Status = status;
        if (status is IncidentStatus.Resolved or IncidentStatus.Closed)
        {
            i.ResolvedAt = DateTime.UtcNow;
            i.ResolutionNote = dto.ResolutionNote;
            // Tài sản liên quan xem như đã sửa xong.
            if (i.AssetId.HasValue)
            {
                var asset = await _db.Assets.FirstOrDefaultAsync(a => a.Id == i.AssetId.Value);
                if (asset != null && asset.Status is AssetStatus.NeedsRepair or AssetStatus.UnderMaintenance)
                {
                    asset.Status = AssetStatus.Good;
                    asset.LastMaintenanceAt = DateTime.UtcNow;
                }
            }
        }
        await _db.SaveChangesAsync();

        await _notify.NotifyUserAsync(i.ReporterId, "Cập nhật sự cố",
            $"Sự cố {i.Code} chuyển sang trạng thái {status}.", "incident", $"/incidents/{i.Id}");

        return await GetByIdAsync(i.Id);
    }

    private static IncidentDto ToDto(Incident i) => new()
    {
        Id = i.Id, Code = i.Code, Title = i.Title, Description = i.Description,
        ReporterId = i.ReporterId, ReporterName = i.Reporter?.FullName ?? "",
        Priority = i.Priority.ToString(), Status = i.Status.ToString(),
        BuildingId = i.BuildingId, ApartmentId = i.ApartmentId, RoomId = i.RoomId, AssetId = i.AssetId,
        AssignedToId = i.AssignedToId, AssignedToName = i.AssignedTo?.FullName,
        ResolvedAt = i.ResolvedAt, ResolutionNote = i.ResolutionNote, PhotoUrl = i.PhotoUrl,
        Category = i.Category, LocationDetail = i.LocationDetail, ContactPhone = i.ContactPhone,
        ExpectedResolutionDate = i.ExpectedResolutionDate, Cost = i.Cost,
        CreatedAt = i.CreatedAt
    };
}
