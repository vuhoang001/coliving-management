using Coliving.Application.DTOs;
using Coliving.Domain.Entities;

namespace Coliving.Application.Interfaces;

/// <summary>Băm và xác thực mật khẩu.</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

/// <summary>Sinh JWT cho người dùng.</summary>
public interface IJwtTokenGenerator
{
    (string token, DateTime expiresAt) Generate(User user);
}

/// <summary>Thông tin người dùng hiện tại lấy từ JWT của request.</summary>
public interface ICurrentUser
{
    int? UserId { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}

/// <summary>Lưu file (ảnh) lên kho đối tượng (MinIO/S3) và trả về URL công khai.</summary>
public interface IFileStorage
{
    Task<string> SaveImageAsync(Stream content, long length, string originalFileName, string contentType,
        string folder = "uploads", CancellationToken ct = default);
}

/// <summary>Đẩy thông báo realtime tới client (SignalR). Hiện thực nằm ở tầng API.</summary>
public interface IRealtimeNotifier
{
    Task PushAsync(int userId, NotificationDto notification);
}
