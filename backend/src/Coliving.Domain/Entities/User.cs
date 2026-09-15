using Coliving.Domain.Common;
using Coliving.Domain.Enums;

namespace Coliving.Domain.Entities;

/// <summary>Tài khoản người dùng: khách thuê, nhân viên, quản lý hoặc admin.</summary>
public class User : BaseEntity
{
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    /// <summary>CMND/CCCD — cần cho hợp đồng thuê.</summary>
    public string? IdentityNumber { get; set; }
    public UserRole Role { get; set; } = UserRole.Tenant;
    public bool IsActive { get; set; } = true;
    public bool EmailConfirmed { get; set; }

    // ---- Hồ sơ khách thuê (bổ sung) ----
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }                // Male | Female | Other
    public string? PermanentAddress { get; set; }      // Địa chỉ thường trú
    public string? Occupation { get; set; }            // Nghề nghiệp
    public string? Nationality { get; set; } = "Việt Nam";
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public DateTime? IdIssueDate { get; set; }         // Ngày cấp CMND/CCCD
    public string? IdIssuePlace { get; set; }          // Nơi cấp

    // Navigation
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}

/// <summary>Refresh token (lưu dạng hash) để cấp lại access token.</summary>
public class RefreshToken : BaseEntity
{
    public int UserId { get; set; }
    public string TokenHash { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public User User { get; set; } = default!;

    public bool IsActive => RevokedAt is null && DateTime.UtcNow < ExpiresAt;
}
