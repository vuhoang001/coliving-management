using System.ComponentModel.DataAnnotations;
using Coliving.Application.Common;

namespace Coliving.Application.DTOs;

public record BuildingDto
{
    public int Id { get; init; }
    public string Name { get; init; } = default!;
    public string Address { get; init; } = default!;
    public string City { get; init; } = default!;
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }
    public int Floors { get; init; }
    public bool IsActive { get; init; }
    public string District { get; init; } = default!;
    public string? Ward { get; init; }
    public string? ContactPhone { get; init; }
    public string? ContactEmail { get; init; }
    public int? YearBuilt { get; init; }
    public double? TotalFloorArea { get; init; }
    public int ParkingSlots { get; init; }
    public bool HasElevator { get; init; }
    public string? Notes { get; init; }
    public int ApartmentCount { get; init; }
    public int RoomCount { get; init; }
}

public record SaveBuildingDto
{
    [Required] public string Name { get; init; } = default!;
    [Required] public string Address { get; init; } = default!;
    [Required] public string City { get; init; } = default!;
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }
    public int Floors { get; init; } = 1;
    public bool IsActive { get; init; } = true;
    [Required] public string District { get; init; } = default!;
    public string? Ward { get; init; }
    public string? ContactPhone { get; init; }
    public string? ContactEmail { get; init; }
    public int? YearBuilt { get; init; }
    public double? TotalFloorArea { get; init; }
    public int ParkingSlots { get; init; }
    public bool HasElevator { get; init; } = true;
    public string? Notes { get; init; }
}

public record ApartmentDto
{
    public int Id { get; init; }
    public int BuildingId { get; init; }
    public string BuildingName { get; init; } = default!;
    public string Code { get; init; } = default!;
    public int Floor { get; init; }
    public double Area { get; init; }
    public int BedroomCount { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public string? Direction { get; init; }
    public string Furnishing { get; init; } = default!;
    public bool HasBalcony { get; init; }
    public decimal MaintenanceFee { get; init; }
    public string? Notes { get; init; }
    public int RoomCount { get; init; }
}

public record SaveApartmentDto
{
    [Required] public int BuildingId { get; init; }
    [Required] public string Code { get; init; } = default!;
    public int Floor { get; init; }
    public double Area { get; init; }
    public int BedroomCount { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
    public string? Direction { get; init; }
    public string Furnishing { get; init; } = "Basic";
    public bool HasBalcony { get; init; }
    public decimal MaintenanceFee { get; init; }
    public string? Notes { get; init; }
}

public record RoomDto
{
    public int Id { get; init; }
    public int ApartmentId { get; init; }
    public string ApartmentCode { get; init; } = default!;
    public string BuildingName { get; init; } = default!;
    public string Code { get; init; } = default!;
    public string Type { get; init; } = default!;
    public int Capacity { get; init; }
    public int CurrentOccupants { get; init; }
    public double Area { get; init; }
    public decimal MonthlyPrice { get; init; }
    public decimal Deposit { get; init; }
    public string Status { get; init; } = default!;
    public string? Description { get; init; }
    public bool HasWindow { get; init; }
    public bool HasPrivateBathroom { get; init; }
    public bool HasAirConditioner { get; init; }
    public decimal ElectricityUnitPrice { get; init; }
    public decimal WaterUnitPrice { get; init; }
    public decimal InternetFee { get; init; }
    public string? ImageUrl { get; init; }
    public string? Notes { get; init; }
}

public record SaveRoomDto
{
    [Required] public int ApartmentId { get; init; }
    [Required] public string Code { get; init; } = default!;
    public string Type { get; init; } = "Private";
    public int Capacity { get; init; } = 1;
    public double Area { get; init; }
    public decimal MonthlyPrice { get; init; }
    public decimal Deposit { get; init; }
    public string? Description { get; init; }
    public bool HasWindow { get; init; } = true;
    public bool HasPrivateBathroom { get; init; }
    public bool HasAirConditioner { get; init; } = true;
    public decimal ElectricityUnitPrice { get; init; } = 3500;
    public decimal WaterUnitPrice { get; init; } = 15000;
    public decimal InternetFee { get; init; } = 100000;
    public string? ImageUrl { get; init; }
    public string? Notes { get; init; }
}

public class RoomFilterDto : PaginationQuery
{
    public int? BuildingId { get; set; }
    public int? ApartmentId { get; set; }
    public string? Type { get; set; }
    public string? Status { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Keyword { get; set; }
}
