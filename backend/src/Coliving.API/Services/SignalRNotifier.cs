using Coliving.API.Hubs;
using Coliving.Application.DTOs;
using Coliving.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Coliving.API.Services;

/// <summary>Hiện thực <see cref="IRealtimeNotifier"/> bằng SignalR.</summary>
public class SignalRNotifier : IRealtimeNotifier
{
    private readonly IHubContext<NotificationHub> _hub;
    public SignalRNotifier(IHubContext<NotificationHub> hub) => _hub = hub;

    public Task PushAsync(int userId, NotificationDto notification)
        => _hub.Clients.User(userId.ToString()).SendAsync("notification", notification);
}
