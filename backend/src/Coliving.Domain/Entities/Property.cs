using Coliving.Domain.Common;
using Coliving.Domain.Enums;

namespace Coliving.Domain.Entities;

/// <summary>Một toà nhà / cơ sở trong chuỗi căn hộ dịch vụ.</summary>
public class Building : BaseEntity
{
    public string Name { get; set; } = default!;
    public string Address { get; set; } = default!;
    public string City { get; set; } = default!;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int Floors { get; set; } = 1;
    public bool IsActive { get; set; } = true;

    // ---- Thông tin bổ sung ----
    public string District { get; set; } = default!;   // Quận/Huyện (bắt buộc)
    public string? Ward { get; set; }                  // Phường/Xã
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public int? YearBuilt { get; set; }
    public double? TotalFloorArea { get; set; }        // Tổng diện tích sàn (m2)
    public int ParkingSlots { get; set; }
    public bool HasElevator { get; set; } = true;
    public string? Notes { get; set; }

    public ICollection<Apartment> Apartments { get; set; } = new List<Apartment>();
    public ICollection<Amenity> Amenities { get; set; } = new List<Amenity>();
    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
}

/// <summary>Căn hộ (unit) trong toà nhà — có thể chứa nhiều phòng cho ở ghép.</summary>
public class Apartment : BaseEntity
{
    public int BuildingId { get; set; }
    public string Code { get; set; } = default!;   // vd A-1203
    public int Floor { get; set; }
    public double Area { get; set; }               // m2
    public int BedroomCount { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // ---- Thông tin bổ sung ----
    public string? Direction { get; set; }             // Hướng: Đông/Tây/Nam/Bắc/Đông Nam...
    public string Furnishing { get; set; } = "Basic";  // Unfurnished | Basic | Full
    public bool HasBalcony { get; set; }
    public decimal MaintenanceFee { get; set; }
    public string? Notes { get; set; }

    public Building Building { get; set; } = default!;
    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}

/// <summary>Phòng / giường cho thuê bên trong một căn hộ (đơn vị đặt chỗ).</summary>
public class Room : BaseEntity
{
    public int ApartmentId { get; set; }
    public string Code { get; set; } = default!;   // vd A-1203-R2
    public RoomType Type { get; set; } = RoomType.Private;
    /// <summary>Số người tối đa (ở ghép > 1).</summary>
    public int Capacity { get; set; } = 1;
    public double Area { get; set; }
    public decimal MonthlyPrice { get; set; }
    public decimal Deposit { get; set; }
    public RoomStatus Status { get; set; } = RoomStatus.Available;
    public string? Description { get; set; }

    // ---- Thông tin bổ sung ----
    public bool HasWindow { get; set; } = true;
    public bool HasPrivateBathroom { get; set; }
    public bool HasAirConditioner { get; set; } = true;
    public decimal ElectricityUnitPrice { get; set; } = 3500;
    public decimal WaterUnitPrice { get; set; } = 15000;
    public decimal InternetFee { get; set; } = 100000;
    public string? ImageUrl { get; set; }
    public string? Notes { get; set; }

    public Apartment Apartment { get; set; } = default!;
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
}
