using System.Security.Claims;
using Coliving.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Coliving.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>Lấy Id người dùng từ JWT, ném 401 nếu chưa đăng nhập.</summary>
    protected int CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw AppException.Unauthorized("Yêu cầu đăng nhập.");

    protected string? CurrentRole => User.FindFirstValue(ClaimTypes.Role);
}
