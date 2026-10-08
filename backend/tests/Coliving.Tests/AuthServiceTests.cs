using System.Text.RegularExpressions;
using Coliving.Application.Common;
using Coliving.Application.DTOs;
using Coliving.Application.Services;
using Coliving.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Coliving.Tests;

public class AuthServiceTests
{
    private static readonly JwtSettings Jwt = new()
    {
        Secret = "unit_test_secret_key_at_least_32_chars_long_000",
        Issuer = "test", Audience = "test", ExpiryMinutes = 60, RefreshTokenDays = 7
    };

    private static (AuthService auth, TestDb db, FakeEmailSender email) Make()
    {
        var db = new TestDb();
        var email = new FakeEmailSender();
        var auth = new AuthService(db.Db, new Coliving.Infrastructure.Services.PasswordHasher(),
            new FakeJwt(), Options.Create(Jwt), email, Options.Create(new EmailSettings()));
        return (auth, db, email);
    }

    [Fact]
    public async Task Register_CreatesTenant_WithHashedPassword_AndToken()
    {
        var (auth, db, _) = Make();
        using var _d = db;

        var res = await auth.RegisterAsync(new RegisterDto { Email = "A@x.com", Password = "secret1", FullName = "An" });

        Assert.Equal("test-token", res.Token);
        var user = await db.Db.Users.SingleAsync();
        Assert.Equal("a@x.com", user.Email);                 // email được chuẩn hoá về chữ thường
        Assert.Equal(UserRole.Tenant, user.Role);            // đăng ký công khai luôn là khách thuê
        Assert.NotEqual("secret1", user.PasswordHash);       // mật khẩu đã được băm
    }

    [Fact]
    public async Task Register_DuplicateEmail_Throws()
    {
        var (auth, db, _) = Make();
        using var _d = db;
        await auth.RegisterAsync(new RegisterDto { Email = "dup@x.com", Password = "secret1", FullName = "A" });

        await Assert.ThrowsAsync<AppException>(() =>
            auth.RegisterAsync(new RegisterDto { Email = "dup@x.com", Password = "secret2", FullName = "B" }));
    }

    [Fact]
    public async Task Login_WrongPassword_Throws()
    {
        var (auth, db, _) = Make();
        using var _d = db;
        await auth.RegisterAsync(new RegisterDto { Email = "u@x.com", Password = "correct1", FullName = "A" });

        await Assert.ThrowsAsync<AppException>(() =>
            auth.LoginAsync(new LoginDto { Email = "u@x.com", Password = "wrong" }));
    }

    [Fact]
    public async Task Login_CorrectPassword_Succeeds()
    {
        var (auth, db, _) = Make();
        using var _d = db;
        await auth.RegisterAsync(new RegisterDto { Email = "u@x.com", Password = "correct1", FullName = "A" });

        var res = await auth.LoginAsync(new LoginDto { Email = "u@x.com", Password = "correct1" });
        Assert.Equal("u@x.com", res.User.Email);
    }

    [Fact]
    public async Task ForgotPassword_UnknownEmail_DoesNothing()
    {
        var (auth, db, email) = Make();
        using var _d = db;

        await auth.ForgotPasswordAsync("ghost@x.com");   // không tiết lộ email tồn tại hay không

        Assert.Empty(email.Sent);
        Assert.Equal(0, await db.Db.UserTokens.CountAsync());
    }

    [Fact]
    public async Task ForgotThenReset_ChangesPassword_AndRevokesRefreshTokens()
    {
        var (auth, db, email) = Make();
        using var _d = db;
        await auth.RegisterAsync(new RegisterDto { Email = "r@x.com", Password = "oldpass1", FullName = "A" });

        await auth.ForgotPasswordAsync("r@x.com");
        Assert.Single(email.Sent);
        var token = Regex.Match(email.Sent[0].Body, @"token=([A-Fa-f0-9]+)").Groups[1].Value;
        Assert.NotEqual("", token);

        await auth.ResetPasswordAsync(token, "newpass1");

        // Refresh token phát ra lúc đăng ký phải bị thu hồi ngay sau khi đặt lại mật khẩu
        // (kiểm tra trước khi đăng nhập lại — vì đăng nhập thành công sẽ tạo token mới).
        Assert.True(await db.Db.RefreshTokens.AllAsync(t => t.RevokedAt != null));

        // Đăng nhập bằng mật khẩu mới phải thành công, mật khẩu cũ phải thất bại.
        var ok = await auth.LoginAsync(new LoginDto { Email = "r@x.com", Password = "newpass1" });
        Assert.Equal("r@x.com", ok.User.Email);
        await Assert.ThrowsAsync<AppException>(() =>
            auth.LoginAsync(new LoginDto { Email = "r@x.com", Password = "oldpass1" }));
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_Throws()
    {
        var (auth, db, _) = Make();
        using var _d = db;
        await Assert.ThrowsAsync<AppException>(() => auth.ResetPasswordAsync("deadbeef", "newpass1"));
    }
}
