using Coliving.Domain.Common;
using Coliving.Domain.Enums;

namespace Coliving.Domain.Entities;

/// <summary>Tài sản / trang thiết bị (nội thất, điều hoà, thiết bị chung...).</summary>
public class Asset : BaseEntity
{
    public string Name { get; set; } = default!;
    /// <summary>Nhóm: Furniture | Appliance | Electronics | HVAC | Safety | Other</summary>
    public string Category { get; set; } = "Other";
    public string? SerialNumber { get; set; }
    public AssetStatus Status { get; set; } = AssetStatus.Good;
    public decimal PurchaseValue { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime? LastMaintenanceAt { get; set; }
    public string? Note { get; set; }

    // ---- Thông tin bổ sung ----
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public int Quantity { get; set; } = 1;
    public DateTime? WarrantyUntil { get; set; }
    public string? Supplier { get; set; }

    // Vị trí (một trong ba cấp): toà nhà (chung), căn hộ, hoặc phòng.
    public int? BuildingId { get; set; }
    public int? ApartmentId { get; set; }
    public int? RoomId { get; set; }

    public Building? Building { get; set; }
    public Apartment? Apartment { get; set; }
    public Room? Room { get; set; }
}
