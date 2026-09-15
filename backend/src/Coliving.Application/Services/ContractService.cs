using Coliving.Application.Common;
using Coliving.Application.DTOs;
using Coliving.Application.Interfaces;
using Coliving.Domain.Entities;
using Coliving.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Coliving.Application.Services;

public class ContractService : IContractService
{
    private readonly IAppDbContext _db;
    private readonly INotificationService _notify;
    public ContractService(IAppDbContext db, INotificationService notify)
    {
        _db = db;
        _notify = notify;
    }

    private IQueryable<Contract> BaseQuery() =>
        _db.Contracts.AsNoTracking()
            .Include(c => c.Tenant)
            .Include(c => c.Booking).ThenInclude(b => b.Room);

    public async Task<List<ContractDto>> GetAllAsync(string? status)
    {
        var q = BaseQuery();
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ContractStatus>(status, true, out var s))
            q = q.Where(c => c.Status == s);
        return await q.OrderByDescending(c => c.CreatedAt).Select(c => ToDto(c)).ToListAsync();
    }

    public async Task<ContractDto> GetByIdAsync(int id)
    {
        var c = await BaseQuery().FirstOrDefaultAsync(x => x.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy hợp đồng.");
        return ToDto(c);
    }

    public async Task<ContractDto> GenerateAsync(int bookingId, GenerateContractDto dto)
    {
        var booking = await _db.Bookings.Include(b => b.Room).Include(b => b.Contract)
            .FirstOrDefaultAsync(b => b.Id == bookingId)
            ?? throw AppException.NotFound("Không tìm thấy đặt phòng.");
        if (booking.Status is BookingStatus.Cancelled or BookingStatus.CheckedOut)
            throw new AppException("Không thể tạo hợp đồng cho đặt phòng đã huỷ/đã trả.");
        if (booking.Contract != null)
            throw AppException.Conflict("Đặt phòng này đã có hợp đồng.");

        var start = dto.StartDate ?? booking.CheckInDate;
        var end = dto.EndDate ?? booking.CheckOutDate;

        var contract = new Contract
        {
            ContractNumber = CodeGenerator.New("HD"),
            BookingId = booking.Id, TenantId = booking.TenantId,
            StartDate = start, EndDate = end,
            MonthlyRent = booking.MonthlyPrice, Deposit = booking.Deposit,
            Status = ContractStatus.PendingSignature,
            Terms = dto.Terms ?? DefaultTerms(booking),
            ContractType = string.IsNullOrWhiteSpace(dto.ContractType) ? "FixedTerm" : dto.ContractType,
            PaymentCycle = string.IsNullOrWhiteSpace(dto.PaymentCycle) ? "Monthly" : dto.PaymentCycle,
            NoticePeriodDays = dto.NoticePeriodDays, LateFeePercent = dto.LateFeePercent,
            UtilitiesIncluded = dto.UtilitiesIncluded, MaxOccupants = Math.Max(1, dto.MaxOccupants),
            DepositPaid = dto.DepositPaid, RenewalTerms = dto.RenewalTerms
        };
        _db.Contracts.Add(contract);
        await _db.SaveChangesAsync();

        await _notify.NotifyUserAsync(booking.TenantId, "Hợp đồng chờ ký",
            $"Hợp đồng {contract.ContractNumber} đã sẵn sàng. Vui lòng ký điện tử.", "contract", $"/contracts/{contract.Id}");

        return await GetByIdAsync(contract.Id);
    }

    public async Task<ContractDto> SignAsync(int userId, int id, SignContractDto dto)
    {
        var c = await _db.Contracts.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy hợp đồng.");
        if (c.TenantId != userId)
            throw AppException.Forbidden("Bạn chỉ có thể ký hợp đồng của mình.");
        if (c.Status != ContractStatus.PendingSignature)
            throw new AppException("Hợp đồng không ở trạng thái chờ ký.");

        c.TenantSignature = dto.Signature;
        c.TenantSignedAt = DateTime.UtcNow;
        c.LandlordSignedAt = DateTime.UtcNow;   // Bên cho thuê ký tự động khi khách hoàn tất.
        c.DocumentUrl = dto.DocumentUrl;
        c.Status = ContractStatus.Active;
        await _db.SaveChangesAsync();

        await _notify.NotifyRoleAsync(UserRole.Manager, "Hợp đồng đã được ký",
            $"Hợp đồng {c.ContractNumber} đã được khách ký và có hiệu lực.", "contract", $"/contracts/{c.Id}");

        return await GetByIdAsync(c.Id);
    }

    public async Task<ContractDto> TerminateAsync(int id, string? reason)
    {
        var c = await _db.Contracts.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy hợp đồng.");
        if (c.Status is ContractStatus.Terminated or ContractStatus.Expired)
            throw new AppException("Hợp đồng đã kết thúc.");
        c.Status = ContractStatus.Terminated;
        if (!string.IsNullOrWhiteSpace(reason))
            c.Terms = $"{c.Terms}\n\n[Chấm dứt] {reason}";
        await _db.SaveChangesAsync();
        return await GetByIdAsync(c.Id);
    }

    private static string DefaultTerms(Booking b) =>
        $"HỢP ĐỒNG THUÊ PHÒNG CO-LIVING\n" +
        $"- Phòng: {b.Room?.Code}\n" +
        $"- Thời hạn: {b.CheckInDate:dd/MM/yyyy} đến {b.CheckOutDate:dd/MM/yyyy}\n" +
        $"- Giá thuê: {b.MonthlyPrice:N0} VNĐ/tháng\n" +
        $"- Tiền cọc: {b.Deposit:N0} VNĐ\n" +
        $"- Bên thuê cam kết tuân thủ nội quy toà nhà, giữ gìn tài sản chung và thanh toán đúng hạn.";

    private static ContractDto ToDto(Contract c) => new()
    {
        Id = c.Id, ContractNumber = c.ContractNumber, BookingId = c.BookingId,
        BookingCode = c.Booking?.Code ?? "", TenantId = c.TenantId, TenantName = c.Tenant?.FullName ?? "",
        RoomCode = c.Booking?.Room?.Code ?? "",
        StartDate = c.StartDate, EndDate = c.EndDate, MonthlyRent = c.MonthlyRent, Deposit = c.Deposit,
        Status = c.Status.ToString(), Terms = c.Terms, TenantSignature = c.TenantSignature,
        TenantSignedAt = c.TenantSignedAt, LandlordSignedAt = c.LandlordSignedAt,
        DocumentUrl = c.DocumentUrl,
        ContractType = c.ContractType, PaymentCycle = c.PaymentCycle,
        NoticePeriodDays = c.NoticePeriodDays, LateFeePercent = c.LateFeePercent,
        UtilitiesIncluded = c.UtilitiesIncluded, MaxOccupants = c.MaxOccupants,
        DepositPaid = c.DepositPaid, RenewalTerms = c.RenewalTerms,
        CreatedAt = c.CreatedAt
    };
}
