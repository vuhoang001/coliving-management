using Coliving.Application.Interfaces;
using Coliving.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Coliving.API.Services;

/// <summary>
/// Dịch vụ nền: định kỳ quét và đánh dấu các hoá đơn đã phát hành nhưng quá hạn thanh toán
/// (Issued/PartiallyPaid + DueDate đã qua + chưa trả đủ) → chuyển trạng thái Overdue và nhắc khách thuê.
/// </summary>
public class InvoiceOverdueService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(30);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InvoiceOverdueService> _logger;

    public InvoiceOverdueService(IServiceScopeFactory scopeFactory, ILogger<InvoiceOverdueService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Chờ một chút cho DB/seed sẵn sàng sau khi khởi động.
        try { await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken); } catch { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ScanAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogWarning(ex, "Quét hoá đơn quá hạn gặp lỗi — bỏ qua vòng này."); }
            try { await Task.Delay(Interval, stoppingToken); } catch { break; }
        }
    }

    private async Task ScanAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var notify = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var now = DateTime.UtcNow;
        var overdue = await db.Invoices
            .Where(i => (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid)
                        && i.DueDate < now && i.PaidAmount < i.Total)
            .ToListAsync(ct);

        if (overdue.Count == 0) return;

        foreach (var inv in overdue) inv.Status = InvoiceStatus.Overdue;
        await db.SaveChangesAsync(ct);

        foreach (var inv in overdue)
            await notify.NotifyUserAsync(inv.TenantId, "Hoá đơn quá hạn",
                $"Hoá đơn {inv.InvoiceNumber} đã quá hạn thanh toán. Vui lòng thanh toán sớm.",
                "invoice", $"/invoices/{inv.Id}");

        _logger.LogInformation("Đã đánh dấu {Count} hoá đơn quá hạn.", overdue.Count);
    }
}
