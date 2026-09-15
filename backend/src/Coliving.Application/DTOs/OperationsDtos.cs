using System.ComponentModel.DataAnnotations;
using Coliving.Application.Common;

namespace Coliving.Application.DTOs;

public record IncidentDto
{
    public int Id { get; init; }
    public string Code { get; init; } = default!;
    public string Title { get; init; } = default!;
    public string Description { get; init; } = default!;
    public int ReporterId { get; init; }
    public string ReporterName { get; init; } = default!;
    public string Priority { get; init; } = default!;
    public string Status { get; init; } = default!;
    public int? BuildingId { get; init; }
    public int? ApartmentId { get; init; }
    public int? RoomId { get; init; }
    public int? AssetId { get; init; }
    public int? AssignedToId { get; init; }
    public string? AssignedToName { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public string? ResolutionNote { get; init; }
    public string? PhotoUrl { get; init; }
    public string Category { get; init; } = default!;
    public string? LocationDetail { get; init; }
    public string? ContactPhone { get; init; }
    public DateTime? ExpectedResolutionDate { get; init; }
    public decimal Cost { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateIncidentDto
{
    [Required] public string Title { get; init; } = default!;
    [Required] public string Description { get; init; } = default!;
    public string Priority { get; init; } = "Medium";
    public int? BuildingId { get; init; }
    public int? ApartmentId { get; init; }
    public int? RoomId { get; init; }
    public int? AssetId { get; init; }
    public string? PhotoUrl { get; init; }
    public string Category { get; init; } = "Other";
    public string? LocationDetail { get; init; }
    public string? ContactPhone { get; init; }
    public DateTime? ExpectedResolutionDate { get; init; }
    public decimal Cost { get; init; }
}

public class IncidentFilterDto : PaginationQuery
{
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public int? BuildingId { get; set; }
    public int? AssignedToId { get; set; }
    public string? Keyword { get; set; }
}

public record UpdateIncidentStatusDto
{
    [Required] public string Status { get; init; } = default!;
    public string? ResolutionNote { get; init; }
}

public record AmenityBookingDto
{
    public int Id { get; init; }
    public int AmenityId { get; init; }
    public string AmenityName { get; init; } = default!;
    public int UserId { get; init; }
    public string UserName { get; init; } = default!;
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public int PartySize { get; init; }
    public decimal Fee { get; init; }
    public string Status { get; init; } = default!;
    public string? Note { get; init; }
}

public record CreateAmenityBookingDto
{
    [Required] public int AmenityId { get; init; }
    [Required] public DateTime StartTime { get; init; }
    [Required] public DateTime EndTime { get; init; }
    public int PartySize { get; init; } = 1;
    public string? Note { get; init; }
}

public record ServiceRequestDto
{
    public int Id { get; init; }
    public string Code { get; init; } = default!;
    public int ServiceCatalogId { get; init; }
    public string ServiceName { get; init; } = default!;
    public int RequesterId { get; init; }
    public string RequesterName { get; init; } = default!;
    public int? RoomId { get; init; }
    public string? RoomCode { get; init; }
    public DateTime ScheduledAt { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TotalPrice { get; init; }
    public string Status { get; init; } = default!;
    public int? AssignedToId { get; init; }
    public string? Note { get; init; }
    public string? ContactPhone { get; init; }
    public string? LocationDetail { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateServiceRequestDto
{
    [Required] public int ServiceCatalogId { get; init; }
    public int? RoomId { get; init; }
    [Required] public DateTime ScheduledAt { get; init; }
    public int Quantity { get; init; } = 1;
    public string? Note { get; init; }
    public string? ContactPhone { get; init; }
    public string? LocationDetail { get; init; }
}
