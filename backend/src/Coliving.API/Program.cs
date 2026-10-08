using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Coliving.API.Extensions;
using Coliving.API.Hubs;
using Coliving.API.Middleware;
using Coliving.API.Services;
using Coliving.Application;
using Coliving.Application.Common;
using Coliving.Application.Interfaces;
using Coliving.Infrastructure;
using Coliving.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---------- Serilog ----------
builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());

// ---------- Config ----------
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;
builder.Services.Configure<MinioSettings>(builder.Configuration.GetSection("Minio"));
builder.Services.Configure<VnPaySettings>(builder.Configuration.GetSection("VnPay"));
var seedSettings = builder.Configuration.GetSection("Seed").Get<SeedSettings>() ?? new SeedSettings();

// ---------- Layers ----------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// ---------- Realtime (SignalR) ----------
builder.Services.AddSignalR();
builder.Services.AddScoped<IRealtimeNotifier, SignalRNotifier>();

// ---------- Dịch vụ nền: tự đánh dấu hoá đơn quá hạn + nhắc khách thuê ----------
builder.Services.AddHostedService<InvoiceOverdueService>();

// ---------- Auth ----------
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
        };
        // WebSocket không gửi header Authorization → cho phép lấy token từ query "access_token" khi gọi /hubs.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    ctx.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

// ---------- Rate limiting (chống dò mật khẩu / spam thanh toán) ----------
builder.Services.AddRateLimiter(options =>
{
    // 10 request/phút cho các endpoint đăng nhập/đăng ký/quên mật khẩu (theo IP).
    options.AddFixedWindowLimiter("auth", o =>
    {
        o.Window = TimeSpan.FromMinutes(1);
        o.PermitLimit = 10;
        o.QueueLimit = 0;
    });
    // 20 request/phút cho các endpoint thanh toán (theo IP).
    options.AddFixedWindowLimiter("payment", o =>
    {
        o.Window = TimeSpan.FromMinutes(1);
        o.PermitLimit = 20;
        o.QueueLimit = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// ---------- CORS ----------
const string CorsPolicy = "AllowFrontend";
builder.Services.AddCors(options => options.AddPolicy(CorsPolicy, policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? new[] { "http://localhost:5175" })
          .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

// ---------- MVC + Swagger ----------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Coliving API", Version = "v1" });
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập token JWT (không cần tiền tố 'Bearer')."
    };
    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ---------- Migrate + Seed (có retry để chịu được DB chưa sẵn sàng lúc khởi động) ----------
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var logger = sp.GetRequiredService<ILogger<Program>>();
    var db = sp.GetRequiredService<AppDbContext>();

    const int maxAttempts = 12;
    for (var attempt = 1; ; attempt++)
    {
        try
        {
            await db.Database.EnsureCreatedAsync();
            // Với DB đã tồn tại (EnsureCreated không chạy lại), tạo bù các bảng mới một cách idempotent.
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS "AuditLogs" (
                    "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                    "UserId" integer NULL,
                    "UserEmail" text NULL,
                    "Action" text NOT NULL DEFAULT '',
                    "EntityType" text NOT NULL DEFAULT '',
                    "EntityId" integer NULL,
                    "Detail" text NULL,
                    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
                    "UpdatedAt" timestamp with time zone NULL,
                    "DeletedAt" timestamp with time zone NULL
                );
                CREATE TABLE IF NOT EXISTS "UserTokens" (
                    "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                    "UserId" integer NOT NULL,
                    "Purpose" integer NOT NULL,
                    "TokenHash" text NOT NULL DEFAULT '',
                    "ExpiresAt" timestamp with time zone NOT NULL DEFAULT now(),
                    "UsedAt" timestamp with time zone NULL,
                    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
                    "UpdatedAt" timestamp with time zone NULL,
                    "DeletedAt" timestamp with time zone NULL
                );
                """);
            await DbSeeder.SeedAsync(db, sp.GetRequiredService<IPasswordHasher>(), seedSettings);
            logger.LogInformation("Khởi tạo & seed cơ sở dữ liệu hoàn tất (lần thử {Attempt}).", attempt);
            break;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(ex, "DB chưa sẵn sàng (lần thử {Attempt}/{Max}), thử lại sau 3s...", attempt, maxAttempts);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}

// ---------- Pipeline ----------
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Coliving API v1"));
}

app.UseCors(CorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
