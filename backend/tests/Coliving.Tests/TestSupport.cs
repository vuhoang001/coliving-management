using Coliving.Application.DTOs;
using Coliving.Application.Interfaces;
using Coliving.Domain.Entities;
using Coliving.Domain.Enums;
using Coliving.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Coliving.Tests;

/// <summary>DB SQLite in-memory cho mỗi test — cô lập, nhanh, hỗ trợ đầy đủ quan hệ.</summary>
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _conn;
    public AppDbContext Db { get; }

    public TestDb()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        Db = new AppDbContext(options);
        Db.Database.EnsureCreated();
    }

    /// <summary>Thêm sẵn một người dùng để test.</summary>
    public User AddUser(string email, string passwordHash = "x", UserRole role = UserRole.Tenant)
    {
        var u = new User { Email = email.ToLowerInvariant(), PasswordHash = passwordHash, FullName = email, Role = role };
        Db.Users.Add(u);
        Db.SaveChanges();
        return u;
    }

    public void Dispose() { Db.Dispose(); _conn.Dispose(); }
}

// ---------- Fakes (thay cho phụ thuộc ngoài phạm vi test) ----------

internal sealed class FakeJwt : IJwtTokenGenerator
{
    public (string token, DateTime expiresAt) Generate(User user) => ("test-token", DateTime.UtcNow.AddHours(1));
}

internal sealed class FakeEmailSender : IEmailSender
{
    public readonly List<(string To, string Subject, string Body)> Sent = new();
    public Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        Sent.Add((toEmail, subject, htmlBody));
        return Task.CompletedTask;
    }
}

internal sealed class FakeNotificationService : INotificationService
{
    public int UserNotifications;
    public Task<NotificationListDto> GetMineAsync(int userId) => Task.FromResult(new NotificationListDto());
    public Task MarkReadAsync(int userId, int id) => Task.CompletedTask;
    public Task MarkAllReadAsync(int userId) => Task.CompletedTask;
    public Task NotifyUserAsync(int userId, string title, string message, string type = "system", string? link = null)
    { UserNotifications++; return Task.CompletedTask; }
    public Task NotifyRoleAsync(UserRole role, string title, string message, string type = "system", string? link = null)
        => Task.CompletedTask;
}
