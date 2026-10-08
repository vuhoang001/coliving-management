using Coliving.Application.Interfaces;
using Coliving.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Coliving.Application;

/// <summary>Đăng ký các service nghiệp vụ của tầng Application.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IBuildingService, BuildingService>();
        services.AddScoped<IApartmentService, ApartmentService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IServiceCatalogService, ServiceCatalogService>();
        services.AddScoped<IAmenityService, AmenityService>();
        services.AddScoped<IAmenityBookingService, AmenityBookingService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IContractService, ContractService>();
        services.AddScoped<ICheckRecordService, CheckRecordService>();
        services.AddScoped<IIncidentService, IncidentService>();
        services.AddScoped<IServiceRequestService, ServiceRequestService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAuditLogger, AuditLogger>();
        return services;
    }
}
