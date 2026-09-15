using Coliving.Application.Common;
using Coliving.Application.DTOs;
using Coliving.Application.Interfaces;
using Coliving.Domain.Entities;
using Coliving.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Coliving.Application.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IAppDbContext _db;
    private readonly INotificationService _notify;
    public InvoiceService(IAppDbContext db, INotificationService notify)
    {
        _db = db;
        _notify = notify;
    }

    private IQueryable<Invoice> FullQuery() =>
        _db.Invoices
            .Include(i => i.Tenant)
            .Include(i => i.Room)
            .Include(i => i.Items)
            .Include(i => i.Shares).ThenInclude(s => s.Tenant)
            .Include(i => i.Payments);

    public async Task<PagedResult<InvoiceDto>> GetAllAsync(PaginationQuery query, string? status)
    {
        var q = FullQuery().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<InvoiceStatus>(status, true, out var s))
            q = q.Where(i => i.Status == s);

        var total = await q.CountAsync();
        var list = await q.OrderByDescending(i => i.IssueDate)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .ToListAsync();
        return new PagedResult<InvoiceDto>
        {
            Items = list.Select(ToDto).ToList(),
            Page = query.Page, PageSize = query.PageSize, TotalItems = total
        };
    }

    public async Task<List<InvoiceDto>> GetMineAsync(int userId)
    {
        // Hoá đơn của tôi = tôi là người đứng tên HOẶC tôi có phần chia (ở ghép).
        var list = await FullQuery().AsNoTracking()
            .Where(i => i.TenantId == userId || i.Shares.Any(s => s.TenantId == userId))
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<InvoiceDto> GetByIdAsync(int actorId, string? role, int id)
    {
        var inv = await FullQuery().AsNoTracking().FirstOrDefaultAsync(i => i.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy hoá đơn.");
        var isManager = role is "Manager" or "Admin" or "Staff";
        var involved = inv.TenantId == actorId || inv.Shares.Any(s => s.TenantId == actorId);
        if (!isManager && !involved)
            throw AppException.Forbidden("Bạn không có quyền xem hoá đơn này.");
        return ToDto(inv);
    }

    public async Task<InvoiceDto> CreateAsync(CreateInvoiceDto dto)
    {
        if (!await _db.Users.AnyAsync(u => u.Id == dto.TenantId))
            throw AppException.NotFound("Không tìm thấy khách thuê.");
        if (dto.PeriodEnd < dto.PeriodStart)
            throw new AppException("Kỳ hoá đơn không hợp lệ.");

        var items = dto.Items.Select(x =>
        {
            if (!Enum.TryParse<InvoiceItemType>(x.Type, true, out var t)) t = InvoiceItemType.Other;
            var qty = x.Quantity <= 0 ? 1 : x.Quantity;
            return new InvoiceItem
            {
                Type = t, Description = x.Description.Trim(),
                Quantity = qty, UnitPrice = x.UnitPrice, Amount = qty * x.UnitPrice,
                Unit = x.Unit, Note = x.Note
            };
        }).ToList();

        var subtotal = items.Sum(i => i.Amount);
        // Total = Subtotal - Discount + Tax, không âm.
        var total = Math.Max(0m, subtotal - dto.Discount + dto.Tax);
        var invoice = new Invoice
        {
            InvoiceNumber = CodeGenerator.New("INV"),
            TenantId = dto.TenantId, RoomId = dto.RoomId, ContractId = dto.ContractId,
            PeriodStart = dto.PeriodStart, PeriodEnd = dto.PeriodEnd,
            IssueDate = DateTime.UtcNow,
            DueDate = dto.DueDate ?? DateTime.UtcNow.AddDays(7),
            Subtotal = subtotal, Total = total, PaidAmount = 0,
            Status = InvoiceStatus.Draft, Note = dto.Note,
            PreviousElectricityReading = dto.PreviousElectricityReading,
            CurrentElectricityReading = dto.CurrentElectricityReading,
            PreviousWaterReading = dto.PreviousWaterReading,
            CurrentWaterReading = dto.CurrentWaterReading,
            Discount = dto.Discount, Tax = dto.Tax,
            Items = items
        };
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();
        return await LoadDto(invoice.Id);
    }

    public async Task<InvoiceDto> IssueAsync(int id)
    {
        var inv = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy hoá đơn.");
        if (inv.Status != InvoiceStatus.Draft)
            throw new AppException("Chỉ hoá đơn nháp mới có thể phát hành.");
        inv.Status = InvoiceStatus.Issued;
        await _db.SaveChangesAsync();

        await _notify.NotifyUserAsync(inv.TenantId, "Hoá đơn mới",
            $"Hoá đơn {inv.InvoiceNumber} ({inv.Total:N0} VNĐ) đã phát hành, hạn {inv.DueDate:dd/MM}.",
            "invoice", $"/invoices/{inv.Id}");
        return await LoadDto(inv.Id);
    }

    public async Task<InvoiceDto> SplitAsync(int id, SplitInvoiceDto dto)
    {
        var inv = await _db.Invoices.Include(i => i.Shares).FirstOrDefaultAsync(i => i.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy hoá đơn.");
        if (dto.Parts.Count == 0)
            throw new AppException("Cần ít nhất một người để chia hoá đơn.");

        var tenantIds = dto.Parts.Select(p => p.TenantId).Distinct().ToList();
        var validCount = await _db.Users.CountAsync(u => tenantIds.Contains(u.Id));
        if (validCount != tenantIds.Count)
            throw AppException.NotFound("Có người ở ghép không tồn tại.");

        // Xoá phần chia cũ, tạo lại.
        _db.InvoiceShares.RemoveRange(inv.Shares);

        var specified = dto.Parts.Where(p => p.Amount.HasValue).ToList();
        var auto = dto.Parts.Where(p => !p.Amount.HasValue).ToList();
        var specifiedSum = specified.Sum(p => p.Amount!.Value);
        if (specifiedSum > inv.Total)
            throw new AppException("Tổng phần chia đã vượt quá giá trị hoá đơn.");

        // Phần còn lại chia đều cho những người không chỉ định số tiền.
        var remaining = inv.Total - specifiedSum;
        var perAuto = auto.Count > 0 ? Math.Round(remaining / auto.Count, 0) : 0m;

        var shares = new List<InvoiceShare>();
        foreach (var p in dto.Parts)
        {
            var amount = p.Amount ?? perAuto;
            shares.Add(new InvoiceShare { InvoiceId = inv.Id, TenantId = p.TenantId, ShareAmount = amount });
        }
        // Bù chênh lệch làm tròn vào người cuối cùng để tổng khớp Total.
        var diff = inv.Total - shares.Sum(s => s.ShareAmount);
        if (diff != 0 && shares.Count > 0) shares[^1].ShareAmount += diff;

        _db.InvoiceShares.AddRange(shares);
        await _db.SaveChangesAsync();

        foreach (var s in shares)
            await _notify.NotifyUserAsync(s.TenantId, "Chia hoá đơn",
                $"Bạn cần thanh toán {s.ShareAmount:N0} VNĐ cho hoá đơn {inv.InvoiceNumber}.",
                "invoice", $"/invoices/{inv.Id}");

        return await LoadDto(inv.Id);
    }

    public async Task DeleteAsync(int id)
    {
        var inv = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy hoá đơn.");
        if (inv.PaidAmount > 0)
            throw AppException.Conflict("Hoá đơn đã có thanh toán, không thể xoá.");
        inv.DeletedAt = DateTime.UtcNow;
        inv.Status = InvoiceStatus.Cancelled;
        await _db.SaveChangesAsync();
    }

    private async Task<InvoiceDto> LoadDto(int id)
    {
        var inv = await FullQuery().AsNoTracking().FirstAsync(i => i.Id == id);
        return ToDto(inv);
    }

    internal static InvoiceDto ToDto(Invoice i) => new()
    {
        Id = i.Id, InvoiceNumber = i.InvoiceNumber, TenantId = i.TenantId,
        TenantName = i.Tenant?.FullName ?? "", RoomId = i.RoomId, RoomCode = i.Room?.Code,
        PeriodStart = i.PeriodStart, PeriodEnd = i.PeriodEnd, IssueDate = i.IssueDate, DueDate = i.DueDate,
        Subtotal = i.Subtotal, Total = i.Total, PaidAmount = i.PaidAmount,
        Status = i.Status.ToString(), Note = i.Note,
        PreviousElectricityReading = i.PreviousElectricityReading,
        CurrentElectricityReading = i.CurrentElectricityReading,
        PreviousWaterReading = i.PreviousWaterReading,
        CurrentWaterReading = i.CurrentWaterReading,
        Discount = i.Discount, Tax = i.Tax,
        Items = i.Items?.Select(x => new InvoiceItemDto
        {
            Id = x.Id, Type = x.Type.ToString(), Description = x.Description,
            Quantity = x.Quantity, UnitPrice = x.UnitPrice, Amount = x.Amount,
            Unit = x.Unit, Note = x.Note
        }).ToList() ?? new(),
        Shares = i.Shares?.Select(s => new InvoiceShareDto
        {
            Id = s.Id, TenantId = s.TenantId, TenantName = s.Tenant?.FullName ?? "",
            ShareAmount = s.ShareAmount, IsPaid = s.IsPaid, PaidAt = s.PaidAt
        }).ToList() ?? new(),
        Payments = i.Payments?.Select(p => new PaymentDto
        {
            Id = p.Id, Amount = p.Amount, Method = p.Method.ToString(),
            Status = p.Status.ToString(), TransactionRef = p.TransactionRef, PaidAt = p.PaidAt
        }).ToList() ?? new()
    };
}
