using Coliving.Application.Common;
using Coliving.Application.Interfaces;
using Coliving.Infrastructure.Persistence;
using Coliving.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Coliving.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ---- DbContext (PostgreSQL qua Npgsql) + expose IAppDbContext cho tầng Application ----
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString, npgsql =>
            // Tự thử lại khi gặp lỗi kết nối tạm thời (DB restart, mạng chập chờn...) → ổn định hơn.
            npgsql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null)));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // ---- Bind cấu hình dùng ở tầng Infrastructure ----
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.Configure<MinioSettings>(configuration.GetSection("Minio"));

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IFileStorage, MinioFileStorage>();

        return services;
    }
}
