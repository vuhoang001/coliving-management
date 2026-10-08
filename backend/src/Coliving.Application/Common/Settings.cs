namespace Coliving.Application.Common;

/// <summary>Cấu hình JWT bind từ appsettings.</summary>
public class JwtSettings
{
    public string Secret { get; set; } = default!;
    public string Issuer { get; set; } = default!;
    public string Audience { get; set; } = default!;
    public int ExpiryMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 7;
}

/// <summary>Cấu hình lưu trữ file trên MinIO (tương thích S3).</summary>
public class MinioSettings
{
    public string Endpoint { get; set; } = "localhost:9000";
    public string PublicEndpoint { get; set; } = "http://localhost:9000";
    public string AccessKey { get; set; } = "minioadmin";
    public string SecretKey { get; set; } = "minioadmin";
    public string Bucket { get; set; } = "coliving";
    public bool UseSsl { get; set; }
}

/// <summary>Cấu hình cổng thanh toán VNPAY (sandbox). IsMock=true khi chưa có credential thật.</summary>
public class VnPaySettings
{
    public string TmnCode { get; set; } = "YOUR_TMNCODE";
    public string HashSecret { get; set; } = "YOUR_HASHSECRET";
    public string BaseUrl { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
    public string ReturnUrl { get; set; } = "http://localhost:8082/api/payments/vnpay/return";
    public string FrontendReturnUrl { get; set; } = "http://localhost:5175/payment/result";
    /// <summary>Trang giả lập cổng VNPAY ở frontend (dùng khi chưa có credential thật).</summary>
    public string MockUrl { get; set; } = "http://localhost:5175/payment/mock";
    public string Version { get; set; } = "2.1.0";
    public string Locale { get; set; } = "vn";
    public bool Mock { get; set; }

    public bool IsMock => Mock || string.IsNullOrWhiteSpace(TmnCode) || TmnCode == "YOUR_TMNCODE";
}

/// <summary>
/// Cấu hình khởi tạo dữ liệu nền tảng (bootstrap): tài khoản admin đầu tiên + dữ liệu demo.
/// Lấy từ appsettings/biến môi trường — KHÔNG hardcode credential trong mã.
/// </summary>
public class SeedSettings
{
    public SeedAccount Admin { get; set; } = new();
    public bool SeedDemoData { get; set; } = true;
}

public class SeedAccount
{
    public string Email { get; set; } = "admin@coliving.local";
    public string Password { get; set; } = "Admin@123";
    public string FullName { get; set; } = "Quản trị hệ thống";
    public string? Phone { get; set; }
}

/// <summary>Cấu hình gửi email. Provider=Log ghi ra console (demo), Smtp gửi thật.</summary>
public class EmailSettings
{
    /// <summary>"Log" (mặc định, ghi console) hoặc "Smtp".</summary>
    public string Provider { get; set; } = "Log";
    public string FromName { get; set; } = "Coliving";
    public string FromEmail { get; set; } = "no-reply@coliving.local";
    public string Host { get; set; } = default!;
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool EnableSsl { get; set; } = true;
    /// <summary>URL frontend để dựng link trong email (đặt lại mật khẩu, xác nhận...).</summary>
    public string AppBaseUrl { get; set; } = "http://localhost:5175";
}
