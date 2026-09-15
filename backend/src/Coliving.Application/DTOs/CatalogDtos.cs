using System.ComponentModel.DataAnnotations;

namespace Coliving.Application.DTOs;

public record ServiceCatalogDto
{
    public int Id { get; init; }
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public string Category { get; init; } = default!;
    public decimal UnitPrice { get; init; }
    public string Unit { get; init; } = default!;
    public bool IsActive { get; init; }
}

public record SaveServiceCatalogDto
{
    [Required] public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public string Category { get; init; } = "Other";
    public decimal UnitPrice { get; init; }
    public string Unit { get; init; } = "lần";
    public bool IsActive { get; init; } = true;
}

public record AmenityDto
{
    public int Id { get; init; }
    public int BuildingId { get; init; }
    public string BuildingName { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public int Capacity { get; init; }
    public int OpenHour { get; init; }
    public int CloseHour { get; init; }
    public int SlotMinutes { get; init; }
    public decimal FeePerSlot { get; init; }
    public bool IsActive { get; init; }
}

public record SaveAmenityDto
{
    [Required] public int BuildingId { get; init; }
    [Required] public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public int Capacity { get; init; } = 1;
    public int OpenHour { get; init; } = 6;
    public int CloseHour { get; init; } = 22;
    public int SlotMinutes { get; init; } = 60;
    public decimal FeePerSlot { get; init; }
    public bool IsActive { get; init; } = true;
}
