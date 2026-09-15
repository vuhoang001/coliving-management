using System.ComponentModel.DataAnnotations;

namespace Coliving.Application.DTOs;

public record BookingDto
{
    public int Id { get; init; }
    public string Code { get; init; } = default!;
    public int RoomId { get; init; }
    public string RoomCode { get; init; } = default!;
    public string BuildingName { get; init; } = default!;
    public int TenantId { get; init; }
    public string TenantName { get; init; } = default!;
    public DateTime CheckInDate { get; init; }
    public DateTime CheckOutDate { get; init; }
    public decimal MonthlyPrice { get; init; }
    public decimal Deposit { get; init; }
    public string Status { get; init; } = default!;
    public string? Note { get; init; }
    public int NumberOfOccupants { get; init; }
    public string? Purpose { get; init; }
    public string SourceChannel { get; init; } = default!;
    public string? VehiclePlate { get; init; }
    public bool HasContract { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateBookingDto
{
    [Required] public int RoomId { get; init; }
    /// <summary>Chỉ quản lý mới được đặt hộ (chỉ định TenantId). Khách bỏ trống để tự đặt.</summary>
    public int? TenantId { get; init; }
    [Required] public DateTime CheckInDate { get; init; }
    [Required] public DateTime CheckOutDate { get; init; }
    public string? Note { get; init; }
    public int NumberOfOccupants { get; init; } = 1;
    public string? Purpose { get; init; }
    public string SourceChannel { get; init; } = "Website";
    public string? VehiclePlate { get; init; }
}

public record ContractDto
{
    public int Id { get; init; }
    public string ContractNumber { get; init; } = default!;
    public int BookingId { get; init; }
    public string BookingCode { get; init; } = default!;
    public int TenantId { get; init; }
    public string TenantName { get; init; } = default!;
    public string RoomCode { get; init; } = default!;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public decimal MonthlyRent { get; init; }
    public decimal Deposit { get; init; }
    public string Status { get; init; } = default!;
    public string? Terms { get; init; }
    public string? TenantSignature { get; init; }
    public DateTime? TenantSignedAt { get; init; }
    public DateTime? LandlordSignedAt { get; init; }
    public string? DocumentUrl { get; init; }
    public string ContractType { get; init; } = default!;
    public string PaymentCycle { get; init; } = default!;
    public int NoticePeriodDays { get; init; }
    public decimal LateFeePercent { get; init; }
    public bool UtilitiesIncluded { get; init; }
    public int MaxOccupants { get; init; }
    public bool DepositPaid { get; init; }
    public string? RenewalTerms { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record GenerateContractDto
{
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? Terms { get; init; }
    public string ContractType { get; init; } = "FixedTerm";
    public string PaymentCycle { get; init; } = "Monthly";
    public int NoticePeriodDays { get; init; } = 30;
    public decimal LateFeePercent { get; init; }
    public bool UtilitiesIncluded { get; init; }
    public int MaxOccupants { get; init; } = 1;
    public bool DepositPaid { get; init; }
    public string? RenewalTerms { get; init; }
}

public record SignContractDto
{
    [Required] public string Signature { get; init; } = default!;
    public string? DocumentUrl { get; init; }
}

public record CheckRecordDto
{
    public int Id { get; init; }
    public int BookingId { get; init; }
    public string BookingCode { get; init; } = default!;
    public string Type { get; init; } = default!;
    public DateTime RecordedAt { get; init; }
    public int? StaffId { get; init; }
    public int? ElectricityMeter { get; init; }
    public int? WaterMeter { get; init; }
    public string? ConditionNote { get; init; }
    public string? PhotoUrl { get; init; }
}

public record CheckActionDto
{
    [Required] public int BookingId { get; init; }
    public int? ElectricityMeter { get; init; }
    public int? WaterMeter { get; init; }
    public string? ConditionNote { get; init; }
    public string? PhotoUrl { get; init; }
}
