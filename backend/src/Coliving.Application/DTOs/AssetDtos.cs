using System.ComponentModel.DataAnnotations;
using Coliving.Application.Common;

namespace Coliving.Application.DTOs;

public record AssetDto
{
    public int Id { get; init; }
    public string Name { get; init; } = default!;
    public string Category { get; init; } = default!;
    public string? SerialNumber { get; init; }
    public string Status { get; init; } = default!;
    public decimal PurchaseValue { get; init; }
    public DateTime? PurchaseDate { get; init; }
    public DateTime? LastMaintenanceAt { get; init; }
    public string? Note { get; init; }
    public string? Brand { get; init; }
    public string? Model { get; init; }
    public int Quantity { get; init; }
    public DateTime? WarrantyUntil { get; init; }
    public string? Supplier { get; init; }
    public int? BuildingId { get; init; }
    public int? ApartmentId { get; init; }
    public int? RoomId { get; init; }
    public string? LocationLabel { get; init; }
}

public record SaveAssetDto
{
    [Required] public string Name { get; init; } = default!;
    public string Category { get; init; } = "Other";
    public string? SerialNumber { get; init; }
    public string Status { get; init; } = "Good";
    public decimal PurchaseValue { get; init; }
    public DateTime? PurchaseDate { get; init; }
    public DateTime? LastMaintenanceAt { get; init; }
    public string? Note { get; init; }
    public string? Brand { get; init; }
    public string? Model { get; init; }
    public int Quantity { get; init; } = 1;
    public DateTime? WarrantyUntil { get; init; }
    public string? Supplier { get; init; }
    public int? BuildingId { get; init; }
    public int? ApartmentId { get; init; }
    public int? RoomId { get; init; }
}

public class AssetFilterDto : PaginationQuery
{
    public string? Category { get; set; }
    public string? Status { get; set; }
    public int? BuildingId { get; set; }
    public int? RoomId { get; set; }
    public string? Keyword { get; set; }
}
