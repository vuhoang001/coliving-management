using Coliving.Application.Common;
using Coliving.Application.DTOs;
using Coliving.Application.Interfaces;
using Coliving.Domain.Entities;
using Coliving.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Coliving.Application.Services;

public class UserService : IUserService
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IAuditLogger _audit;
    public UserService(IAppDbContext db, IPasswordHasher hasher, IAuditLogger audit)
    {
        _db = db;
        _hasher = hasher;
        _audit = audit;
    }

    public async Task<PagedResult<UserDto>> GetAllAsync(PaginationQuery query, string? role, string? keyword)
    {
        var q = _db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<UserRole>(role, true, out var r))
            q = q.Where(u => u.Role == r);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim().ToLower();
            q = q.Where(u => u.FullName.ToLower().Contains(k) || u.Email.ToLower().Contains(k)
                || (u.Phone != null && u.Phone.Contains(k)));
        }

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(u => u.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(u => AuthService.ToDto(u))
            .ToListAsync();

        return new PagedResult<UserDto>
        {
            Items = items, Page = query.Page, PageSize = query.PageSize, TotalItems = total
        };
    }

    public async Task<UserDto> GetByIdAsync(int id)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy người dùng.");
        return AuthService.ToDto(user);
    }

    public async Task<UserDto> CreateAsync(CreateUserDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email))
            throw AppException.Conflict("Email đã được sử dụng.");
        if (!Enum.TryParse<UserRole>(dto.Role, true, out var role))
            throw new AppException("Vai trò không hợp lệ.");

        var user = new User
        {
            Email = email,
            PasswordHash = _hasher.Hash(dto.Password),
            FullName = dto.FullName.Trim(),
            Phone = dto.Phone,
            IdentityNumber = dto.IdentityNumber,
            Role = role,
            EmailConfirmed = true,
            DateOfBirth = dto.DateOfBirth, Gender = dto.Gender,
            PermanentAddress = dto.PermanentAddress, Occupation = dto.Occupation,
            Nationality = dto.Nationality, EmergencyContactName = dto.EmergencyContactName,
            EmergencyContactPhone = dto.EmergencyContactPhone,
            IdIssueDate = dto.IdIssueDate, IdIssuePlace = dto.IdIssuePlace
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Create", nameof(User), user.Id, $"Tạo người dùng {user.Email} (vai trò {role}).");
        await _db.SaveChangesAsync();
        return AuthService.ToDto(user);
    }

    public async Task<UserDto> UpdateAsync(int id, UpdateUserDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy người dùng.");
        if (!Enum.TryParse<UserRole>(dto.Role, true, out var role))
            throw new AppException("Vai trò không hợp lệ.");

        user.FullName = dto.FullName.Trim();
        user.Phone = dto.Phone;
        user.IdentityNumber = dto.IdentityNumber;
        user.Role = role;
        user.DateOfBirth = dto.DateOfBirth; user.Gender = dto.Gender;
        user.PermanentAddress = dto.PermanentAddress; user.Occupation = dto.Occupation;
        user.Nationality = dto.Nationality; user.EmergencyContactName = dto.EmergencyContactName;
        user.EmergencyContactPhone = dto.EmergencyContactPhone;
        user.IdIssueDate = dto.IdIssueDate; user.IdIssuePlace = dto.IdIssuePlace;
        await _audit.LogAsync("Update", nameof(User), user.Id, $"Cập nhật người dùng {user.Email} (vai trò {role}).");
        await _db.SaveChangesAsync();
        return AuthService.ToDto(user);
    }

    public async Task SetActiveAsync(int id, bool isActive)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy người dùng.");
        user.IsActive = isActive;
        await _audit.LogAsync("SetActive", nameof(User), user.Id,
            $"{(isActive ? "Kích hoạt" : "Vô hiệu hoá")} tài khoản {user.Email}.");
        await _db.SaveChangesAsync();
    }
}
