using System.Security.Cryptography;
using System.Text;
using Coliving.Application.Common;
using Coliving.Application.DTOs;
using Coliving.Application.Interfaces;
using Coliving.Domain.Entities;
using Coliving.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Coliving.Application.Services;

public class AuthService : IAuthService
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenGenerator _jwt;
    private readonly JwtSettings _jwtSettings;

    public AuthService(IAppDbContext db, IPasswordHasher hasher, IJwtTokenGenerator jwt, IOptions<JwtSettings> jwtSettings)
    {
        _db = db;
        _hasher = hasher;
        _jwt = jwt;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email))
            throw AppException.Conflict("Email đã được sử dụng.");

        var user = new User
        {
            Email = email,
            PasswordHash = _hasher.Hash(dto.Password),
            FullName = dto.FullName.Trim(),
            Phone = dto.Phone,
            Role = UserRole.Tenant,   // Đăng ký công khai luôn là khách thuê.
            EmailConfirmed = true
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return await BuildAuthResponse(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email)
            ?? throw AppException.Unauthorized("Email hoặc mật khẩu không đúng.");

        if (!user.IsActive)
            throw AppException.Forbidden("Tài khoản đã bị vô hiệu hoá.");
        if (!_hasher.Verify(dto.Password, user.PasswordHash))
            throw AppException.Unauthorized("Email hoặc mật khẩu không đúng.");

        return await BuildAuthResponse(user);
    }

    public async Task<UserDto> GetProfileAsync(int userId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw AppException.NotFound("Không tìm thấy người dùng.");
        return ToDto(user);
    }

    public async Task<UserDto> UpdateProfileAsync(int userId, UpdateProfileDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw AppException.NotFound("Không tìm thấy người dùng.");
        user.FullName = dto.FullName.Trim();
        user.Phone = dto.Phone;
        user.AvatarUrl = dto.AvatarUrl;
        user.IdentityNumber = dto.IdentityNumber;
        user.DateOfBirth = dto.DateOfBirth; user.Gender = dto.Gender;
        user.PermanentAddress = dto.PermanentAddress; user.Occupation = dto.Occupation;
        user.Nationality = dto.Nationality; user.EmergencyContactName = dto.EmergencyContactName;
        user.EmergencyContactPhone = dto.EmergencyContactPhone;
        user.IdIssueDate = dto.IdIssueDate; user.IdIssuePlace = dto.IdIssuePlace;
        await _db.SaveChangesAsync();
        return ToDto(user);
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw AppException.NotFound("Không tìm thấy người dùng.");
        if (!_hasher.Verify(dto.CurrentPassword, user.PasswordHash))
            throw new AppException("Mật khẩu hiện tại không đúng.");
        user.PasswordHash = _hasher.Hash(dto.NewPassword);
        await _db.SaveChangesAsync();
    }

    public async Task<AuthResponseDto> RefreshAsync(string refreshToken)
    {
        var hash = HashToken(refreshToken);
        var stored = await _db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == hash)
            ?? throw AppException.Unauthorized("Refresh token không hợp lệ.");

        if (!stored.IsActive)
            throw AppException.Unauthorized("Refresh token đã hết hạn hoặc bị thu hồi.");

        // Xoay vòng: thu hồi token cũ, phát token mới.
        stored.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return await BuildAuthResponse(stored.User);
    }

    public async Task LogoutAsync(string? refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;
        var hash = HashToken(refreshToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hash);
        if (stored is { RevokedAt: null })
        {
            stored.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    private async Task<AuthResponseDto> BuildAuthResponse(User user)
    {
        var (token, expiresAt) = _jwt.Generate(user);

        var rawRefresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(rawRefresh),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays)
        });
        await _db.SaveChangesAsync();

        return new AuthResponseDto
        {
            Token = token,
            RefreshToken = rawRefresh,
            ExpiresAt = expiresAt,
            User = ToDto(user)
        };
    }

    private static string HashToken(string token)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    internal static UserDto ToDto(User u) => new()
    {
        Id = u.Id,
        Email = u.Email,
        FullName = u.FullName,
        Phone = u.Phone,
        AvatarUrl = u.AvatarUrl,
        IdentityNumber = u.IdentityNumber,
        Role = u.Role.ToString(),
        IsActive = u.IsActive,
        EmailConfirmed = u.EmailConfirmed,
        DateOfBirth = u.DateOfBirth, Gender = u.Gender,
        PermanentAddress = u.PermanentAddress, Occupation = u.Occupation,
        Nationality = u.Nationality, EmergencyContactName = u.EmergencyContactName,
        EmergencyContactPhone = u.EmergencyContactPhone,
        IdIssueDate = u.IdIssueDate, IdIssuePlace = u.IdIssuePlace,
        CreatedAt = u.CreatedAt
    };
}
