using Coliving.Application.DTOs;
using Coliving.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Coliving.API.Controllers;

public class AuthController : BaseApiController
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    [EnableRateLimiting("auth")]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
        => Ok(await _auth.RegisterAsync(dto));

    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
        => Ok(await _auth.LoginAsync(dto));

    [EnableRateLimiting("auth")]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> Refresh(RefreshRequestDto dto)
        => Ok(await _auth.RefreshAsync(dto.RefreshToken));

    [EnableRateLimiting("auth")]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
    {
        await _auth.ForgotPasswordAsync(dto.Email);
        return Ok(new { message = "Nếu email tồn tại, hệ thống đã gửi liên kết đặt lại mật khẩu." });
    }

    [EnableRateLimiting("auth")]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
    {
        await _auth.ResetPasswordAsync(dto.Token, dto.NewPassword);
        return Ok(new { message = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập lại." });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshRequestDto dto)
    {
        await _auth.LogoutAsync(dto.RefreshToken);
        return Ok(new { message = "Đã đăng xuất." });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me() => Ok(await _auth.GetProfileAsync(CurrentUserId));

    [Authorize]
    [HttpPut("profile")]
    public async Task<ActionResult<UserDto>> UpdateProfile(UpdateProfileDto dto)
        => Ok(await _auth.UpdateProfileAsync(CurrentUserId, dto));

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        await _auth.ChangePasswordAsync(CurrentUserId, dto);
        return Ok(new { message = "Đổi mật khẩu thành công." });
    }
}
