using Coliving.Application.Common;
using Coliving.Application.DTOs;
using Coliving.Application.Interfaces;
using Coliving.Domain.Entities;
using Coliving.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Coliving.Application.Services;

public class ServiceRequestService : IServiceRequestService
{
    private readonly IAppDbContext _db;
    private readonly INotificationService _notify;
    public ServiceRequestService(IAppDbContext db, INotificationService notify)
    {
        _db = db;
        _notify = notify;
    }

    private IQueryable<ServiceRequest> BaseQuery() =>
        _db.ServiceRequests.AsNoTracking()
            .Include(s => s.Service).Include(s => s.Requester).Include(s => s.Room);

    public async Task<PagedResult<ServiceRequestDto>> GetAllAsync(PaginationQuery query, string? status)
    {
        var q = BaseQuery();
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ServiceRequestStatus>(status, true, out var s))
            q = q.Where(x => x.Status == s);

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(x => x.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => ToDto(x))
            .ToListAsync();
        return new PagedResult<ServiceRequestDto>
        {
            Items = items, Page = query.Page, PageSize = query.PageSize, TotalItems = total
        };
    }

    public async Task<List<ServiceRequestDto>> GetMineAsync(int userId)
        => await BaseQuery().Where(s => s.RequesterId == userId)
            .OrderByDescending(s => s.CreatedAt).Select(s => ToDto(s)).ToListAsync();

    public async Task<ServiceRequestDto> CreateAsync(int userId, CreateServiceRequestDto dto)
    {
        var service = await _db.ServiceCatalogs.FirstOrDefaultAsync(s => s.Id == dto.ServiceCatalogId && s.IsActive)
            ?? throw AppException.NotFound("Không tìm thấy dịch vụ hoặc dịch vụ đang tạm ngưng.");
        var qty = Math.Max(1, dto.Quantity);

        var req = new ServiceRequest
        {
            Code = CodeGenerator.New("SR"),
            ServiceCatalogId = service.Id, RequesterId = userId, RoomId = dto.RoomId,
            ScheduledAt = dto.ScheduledAt, Quantity = qty,
            UnitPrice = service.UnitPrice, TotalPrice = service.UnitPrice * qty,
            Status = ServiceRequestStatus.Requested, Note = dto.Note,
            ContactPhone = dto.ContactPhone, LocationDetail = dto.LocationDetail
        };
        _db.ServiceRequests.Add(req);
        await _db.SaveChangesAsync();

        await _notify.NotifyRoleAsync(UserRole.Staff, "Yêu cầu dịch vụ mới",
            $"{service.Name} x{qty} ({req.Code})", "service", $"/service-requests/{req.Id}");

        return await LoadDto(req.Id);
    }

    public async Task<ServiceRequestDto> UpdateStatusAsync(int id, string status, int? assignedToId)
    {
        var req = await _db.ServiceRequests.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy yêu cầu dịch vụ.");
        if (!Enum.TryParse<ServiceRequestStatus>(status, true, out var s))
            throw new AppException("Trạng thái không hợp lệ.");
        req.Status = s;
        if (assignedToId.HasValue) req.AssignedToId = assignedToId;
        await _db.SaveChangesAsync();

        await _notify.NotifyUserAsync(req.RequesterId, "Cập nhật yêu cầu dịch vụ",
            $"Yêu cầu {req.Code} chuyển sang {s}.", "service", $"/service-requests/{req.Id}");

        return await LoadDto(req.Id);
    }

    public async Task CancelAsync(int userId, string? role, int id)
    {
        var req = await _db.ServiceRequests.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy yêu cầu dịch vụ.");
        var isManager = role is "Manager" or "Admin" or "Staff";
        if (req.RequesterId != userId && !isManager)
            throw AppException.Forbidden("Bạn không có quyền huỷ yêu cầu này.");
        if (req.Status is ServiceRequestStatus.Completed or ServiceRequestStatus.Cancelled)
            throw new AppException("Yêu cầu không thể huỷ ở trạng thái hiện tại.");
        req.Status = ServiceRequestStatus.Cancelled;
        await _db.SaveChangesAsync();
    }

    private async Task<ServiceRequestDto> LoadDto(int id)
    {
        var s = await BaseQuery().FirstAsync(x => x.Id == id);
        return ToDto(s);
    }

    private static ServiceRequestDto ToDto(ServiceRequest s) => new()
    {
        Id = s.Id, Code = s.Code, ServiceCatalogId = s.ServiceCatalogId,
        ServiceName = s.Service?.Name ?? "", RequesterId = s.RequesterId,
        RequesterName = s.Requester?.FullName ?? "", RoomId = s.RoomId, RoomCode = s.Room?.Code,
        ScheduledAt = s.ScheduledAt, Quantity = s.Quantity, UnitPrice = s.UnitPrice,
        TotalPrice = s.TotalPrice, Status = s.Status.ToString(),
        AssignedToId = s.AssignedToId, Note = s.Note,
        ContactPhone = s.ContactPhone, LocationDetail = s.LocationDetail,
        CreatedAt = s.CreatedAt
    };
}
