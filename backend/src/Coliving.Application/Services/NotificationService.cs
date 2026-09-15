using Coliving.Application.DTOs;
using Coliving.Application.Interfaces;
using Coliving.Domain.Entities;
using Coliving.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Coliving.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IAppDbContext _db;
    private readonly IRealtimeNotifier _realtime;
    public NotificationService(IAppDbContext db, IRealtimeNotifier realtime)
    {
        _db = db;
        _realtime = realtime;
    }

    public async Task<NotificationListDto> GetMineAsync(int userId)
    {
        var items = await _db.Notifications.AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => ToDto(n))
            .ToListAsync();
        var unread = await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
        return new NotificationListDto { Items = items, UnreadCount = unread };
    }

    public async Task MarkReadAsync(int userId, int id)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (n is { IsRead: false })
        {
            n.IsRead = true;
            await _db.SaveChangesAsync();
        }
    }

    public async Task MarkAllReadAsync(int userId)
    {
        var unread = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
        foreach (var n in unread) n.IsRead = true;
        await _db.SaveChangesAsync();
    }

    public async Task NotifyUserAsync(int userId, string title, string message, string type = "system", string? link = null)
    {
        var n = new Notification { UserId = userId, Title = title, Message = message, Type = type, Link = link };
        _db.Notifications.Add(n);
        await _db.SaveChangesAsync();
        await SafePush(userId, n);
    }

    public async Task NotifyRoleAsync(UserRole role, string title, string message, string type = "system", string? link = null)
    {
        var userIds = await _db.Users.AsNoTracking()
            .Where(u => u.Role == role && u.IsActive)
            .Select(u => u.Id).ToListAsync();
        if (userIds.Count == 0) return;

        var list = userIds
            .Select(id => new Notification { UserId = id, Title = title, Message = message, Type = type, Link = link })
            .ToList();
        _db.Notifications.AddRange(list);
        await _db.SaveChangesAsync();
        foreach (var n in list) await SafePush(n.UserId, n);
    }

    private async Task SafePush(int userId, Notification n)
    {
        try { await _realtime.PushAsync(userId, ToDto(n)); }
        catch { /* realtime là best-effort; DB vẫn là nguồn sự thật */ }
    }

    private static NotificationDto ToDto(Notification n) => new()
    {
        Id = n.Id, Title = n.Title, Message = n.Message, Type = n.Type,
        Link = n.Link, IsRead = n.IsRead, CreatedAt = n.CreatedAt
    };
}
