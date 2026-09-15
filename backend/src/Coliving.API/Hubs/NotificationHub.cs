using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Coliving.API.Hubs;

/// <summary>
/// Hub thông báo realtime (server → client). Client kết nối và lắng nghe sự kiện "notification".
/// SignalR nhóm kết nối theo claim NameIdentifier (userId) nên Clients.User(userId) tới đúng người.
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
}
