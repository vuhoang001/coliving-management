using Coliving.Application.Common;
using Coliving.Application.DTOs;

namespace Coliving.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<UserDto> GetProfileAsync(int userId);
    Task<UserDto> UpdateProfileAsync(int userId, UpdateProfileDto dto);
    Task ChangePasswordAsync(int userId, ChangePasswordDto dto);
    Task<AuthResponseDto> RefreshAsync(string refreshToken);
    Task LogoutAsync(string? refreshToken);
}

/// <summary>Quản lý người dùng (khách thuê / nhân viên) — dành cho quản lý.</summary>
public interface IUserService
{
    Task<PagedResult<UserDto>> GetAllAsync(PaginationQuery query, string? role, string? keyword);
    Task<UserDto> GetByIdAsync(int id);
    Task<UserDto> CreateAsync(CreateUserDto dto);
    Task<UserDto> UpdateAsync(int id, UpdateUserDto dto);
    Task SetActiveAsync(int id, bool isActive);
}

public interface IBuildingService
{
    Task<List<BuildingDto>> GetAllAsync();
    Task<BuildingDto> GetByIdAsync(int id);
    Task<BuildingDto> CreateAsync(SaveBuildingDto dto);
    Task<BuildingDto> UpdateAsync(int id, SaveBuildingDto dto);
    Task DeleteAsync(int id);
}

public interface IApartmentService
{
    Task<List<ApartmentDto>> GetAllAsync(int? buildingId);
    Task<ApartmentDto> GetByIdAsync(int id);
    Task<ApartmentDto> CreateAsync(SaveApartmentDto dto);
    Task<ApartmentDto> UpdateAsync(int id, SaveApartmentDto dto);
    Task DeleteAsync(int id);
}

public interface IRoomService
{
    Task<PagedResult<RoomDto>> SearchAsync(RoomFilterDto filter);
    Task<List<RoomDto>> GetAvailableAsync(DateTime? from, DateTime? to);
    Task<RoomDto> GetByIdAsync(int id);
    Task<RoomDto> CreateAsync(SaveRoomDto dto);
    Task<RoomDto> UpdateAsync(int id, SaveRoomDto dto);
    Task<RoomDto> SetStatusAsync(int id, string status);
    Task DeleteAsync(int id);
}

public interface IAssetService
{
    Task<PagedResult<AssetDto>> SearchAsync(AssetFilterDto filter);
    Task<AssetDto> GetByIdAsync(int id);
    Task<AssetDto> CreateAsync(SaveAssetDto dto);
    Task<AssetDto> UpdateAsync(int id, SaveAssetDto dto);
    Task DeleteAsync(int id);
}

public interface IServiceCatalogService
{
    Task<List<ServiceCatalogDto>> GetAllAsync(bool onlyActive);
    Task<ServiceCatalogDto> CreateAsync(SaveServiceCatalogDto dto);
    Task<ServiceCatalogDto> UpdateAsync(int id, SaveServiceCatalogDto dto);
    Task DeleteAsync(int id);
}

public interface IAmenityService
{
    Task<List<AmenityDto>> GetAllAsync(int? buildingId);
    Task<AmenityDto> CreateAsync(SaveAmenityDto dto);
    Task<AmenityDto> UpdateAsync(int id, SaveAmenityDto dto);
    Task DeleteAsync(int id);
}

public interface IAmenityBookingService
{
    Task<List<AmenityBookingDto>> GetByAmenityAsync(int amenityId, DateTime day);
    Task<List<AmenityBookingDto>> GetMineAsync(int userId);
    Task<AmenityBookingDto> BookAsync(int userId, CreateAmenityBookingDto dto);
    Task CancelAsync(int userId, string? role, int id);
}

public interface IBookingService
{
    Task<PagedResult<BookingDto>> GetAllAsync(PaginationQuery query, string? status);
    Task<List<BookingDto>> GetMineAsync(int userId);
    Task<BookingDto> GetByIdAsync(int id);
    Task<BookingDto> CreateAsync(int actorId, string? role, CreateBookingDto dto);
    Task<BookingDto> ConfirmAsync(int id);
    Task<BookingDto> CancelAsync(int actorId, string? role, int id, string? reason);
}

public interface IContractService
{
    Task<List<ContractDto>> GetAllAsync(string? status);
    Task<ContractDto> GetByIdAsync(int id);
    Task<ContractDto> GenerateAsync(int bookingId, GenerateContractDto dto);
    Task<ContractDto> SignAsync(int userId, int id, SignContractDto dto);
    Task<ContractDto> TerminateAsync(int id, string? reason);
}

/// <summary>Nhận phòng / trả phòng.</summary>
public interface ICheckRecordService
{
    Task<List<CheckRecordDto>> GetByBookingAsync(int bookingId);
    Task<CheckRecordDto> CheckInAsync(int staffId, CheckActionDto dto);
    Task<CheckRecordDto> CheckOutAsync(int staffId, CheckActionDto dto);
}

public interface IIncidentService
{
    Task<PagedResult<IncidentDto>> GetAllAsync(IncidentFilterDto filter);
    Task<List<IncidentDto>> GetMineAsync(int userId);
    Task<IncidentDto> GetByIdAsync(int id);
    Task<IncidentDto> CreateAsync(int reporterId, CreateIncidentDto dto);
    Task<IncidentDto> AssignAsync(int id, int staffId);
    Task<IncidentDto> UpdateStatusAsync(int id, UpdateIncidentStatusDto dto);
}

public interface IServiceRequestService
{
    Task<PagedResult<ServiceRequestDto>> GetAllAsync(PaginationQuery query, string? status);
    Task<List<ServiceRequestDto>> GetMineAsync(int userId);
    Task<ServiceRequestDto> CreateAsync(int userId, CreateServiceRequestDto dto);
    Task<ServiceRequestDto> UpdateStatusAsync(int id, string status, int? assignedToId);
    Task CancelAsync(int userId, string? role, int id);
}

public interface IInvoiceService
{
    Task<PagedResult<InvoiceDto>> GetAllAsync(PaginationQuery query, string? status);
    Task<List<InvoiceDto>> GetMineAsync(int userId);
    Task<InvoiceDto> GetByIdAsync(int actorId, string? role, int id);
    Task<InvoiceDto> CreateAsync(CreateInvoiceDto dto);
    Task<InvoiceDto> IssueAsync(int id);
    /// <summary>Chia đều/không đều hoá đơn cho nhiều người ở ghép.</summary>
    Task<InvoiceDto> SplitAsync(int id, SplitInvoiceDto dto);
    Task DeleteAsync(int id);
}

public interface IPaymentService
{
    Task<PaymentResultDto> PayAsync(int actorId, PayInvoiceDto dto);
    Task<CreatePaymentDto> CreateVnPayUrlAsync(int actorId, int invoiceId, string ipAddress);
    Task<PaymentResultDto> HandleVnPayReturnAsync(IReadOnlyDictionary<string, string> query, bool isIpn = false);
    Task<PaymentResultDto> CompleteMockAsync(int actorId, int invoiceId, bool success);
}

public interface INotificationService
{
    Task<NotificationListDto> GetMineAsync(int userId);
    Task MarkReadAsync(int userId, int id);
    Task MarkAllReadAsync(int userId);
    Task NotifyUserAsync(int userId, string title, string message, string type = "system", string? link = null);
    Task NotifyRoleAsync(Domain.Enums.UserRole role, string title, string message, string type = "system", string? link = null);
}

public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync();
    Task<RevenueReportDto> GetRevenueAsync(int months);
    Task<OccupancyReportDto> GetOccupancyAsync();
}
