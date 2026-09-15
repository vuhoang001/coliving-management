using Coliving.Domain.Common;

namespace Coliving.Domain.Entities;

/// <summary>Thông báo gửi tới người dùng (đặt phòng, hoá đơn, sự cố, hợp đồng...).</summary>
public class Notification : BaseEntity
{
    public int UserId { get; set; }
    public string Title { get; set; } = default!;
    public string Message { get; set; } = default!;
    /// <summary>system | booking | invoice | incident | contract | amenity | service</summary>
    public string Type { get; set; } = "system";
    public string? Link { get; set; }
    public bool IsRead { get; set; }

    public User User { get; set; } = default!;
}
