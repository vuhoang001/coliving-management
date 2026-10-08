using Coliving.Application.Common;
using Coliving.Application.DTOs;
using Coliving.Application.Services;
using Coliving.Domain.Entities;
using Coliving.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Coliving.Tests;

/// <summary>Kiểm thử nghiệp vụ chia tiền hoá đơn cho người ở ghép — phần quan trọng nhất.</summary>
public class InvoiceSplitTests
{
    private static Invoice SeedInvoice(TestDb db, decimal total, int tenantId)
    {
        var inv = new Invoice
        {
            InvoiceNumber = "INV-TEST", TenantId = tenantId,
            PeriodStart = DateTime.UtcNow, PeriodEnd = DateTime.UtcNow.AddDays(30),
            IssueDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(7),
            Subtotal = total, Total = total, Status = InvoiceStatus.Issued
        };
        db.Db.Invoices.Add(inv);
        db.Db.SaveChanges();
        return inv;
    }

    private static InvoiceService Make(TestDb db) => new(db.Db, new FakeNotificationService());

    [Fact]
    public async Task Split_Evenly_AcrossThreeTenants()
    {
        using var db = new TestDb();
        var t1 = db.AddUser("t1@x.com"); var t2 = db.AddUser("t2@x.com"); var t3 = db.AddUser("t3@x.com");
        var inv = SeedInvoice(db, 3_000_000m, t1.Id);

        var res = await Make(db).SplitAsync(inv.Id, new SplitInvoiceDto
        {
            Parts = new() { new() { TenantId = t1.Id }, new() { TenantId = t2.Id }, new() { TenantId = t3.Id } }
        });

        Assert.Equal(3, res.Shares.Count);
        Assert.All(res.Shares, s => Assert.Equal(1_000_000m, s.ShareAmount));
        Assert.Equal(3_000_000m, res.Shares.Sum(s => s.ShareAmount));  // tổng phải khớp Total
    }

    [Fact]
    public async Task Split_Mixed_SpecifiedAndAuto_RemainderSharedEvenly()
    {
        using var db = new TestDb();
        var t1 = db.AddUser("t1@x.com"); var t2 = db.AddUser("t2@x.com"); var t3 = db.AddUser("t3@x.com");
        var inv = SeedInvoice(db, 3_000_000m, t1.Id);

        var res = await Make(db).SplitAsync(inv.Id, new SplitInvoiceDto
        {
            Parts = new()
            {
                new() { TenantId = t1.Id, Amount = 1_000_000m },  // chỉ định
                new() { TenantId = t2.Id },                       // chia đều phần còn lại
                new() { TenantId = t3.Id }
            }
        });

        Assert.Equal(1_000_000m, res.Shares.Single(s => s.TenantId == t1.Id).ShareAmount);
        Assert.Equal(1_000_000m, res.Shares.Single(s => s.TenantId == t2.Id).ShareAmount);
        Assert.Equal(1_000_000m, res.Shares.Single(s => s.TenantId == t3.Id).ShareAmount);
        Assert.Equal(3_000_000m, res.Shares.Sum(s => s.ShareAmount));
    }

    [Fact]
    public async Task Split_RoundingDifference_AbsorbedByLastShare()
    {
        using var db = new TestDb();
        var t1 = db.AddUser("t1@x.com"); var t2 = db.AddUser("t2@x.com"); var t3 = db.AddUser("t3@x.com");
        var inv = SeedInvoice(db, 1_000_000m, t1.Id);   // chia 3 không chẵn

        var res = await Make(db).SplitAsync(inv.Id, new SplitInvoiceDto
        {
            Parts = new() { new() { TenantId = t1.Id }, new() { TenantId = t2.Id }, new() { TenantId = t3.Id } }
        });

        // Dù làm tròn từng phần, tổng vẫn phải đúng bằng Total (chênh lệch dồn vào người cuối).
        Assert.Equal(1_000_000m, res.Shares.Sum(s => s.ShareAmount));
    }

    [Fact]
    public async Task Split_SpecifiedExceedsTotal_Throws()
    {
        using var db = new TestDb();
        var t1 = db.AddUser("t1@x.com"); var t2 = db.AddUser("t2@x.com");
        var inv = SeedInvoice(db, 1_000_000m, t1.Id);

        await Assert.ThrowsAsync<AppException>(() => Make(db).SplitAsync(inv.Id, new SplitInvoiceDto
        {
            Parts = new() { new() { TenantId = t1.Id, Amount = 2_000_000m }, new() { TenantId = t2.Id } }
        }));
    }

    [Fact]
    public async Task Split_UnknownTenant_Throws()
    {
        using var db = new TestDb();
        var t1 = db.AddUser("t1@x.com");
        var inv = SeedInvoice(db, 1_000_000m, t1.Id);

        await Assert.ThrowsAsync<AppException>(() => Make(db).SplitAsync(inv.Id, new SplitInvoiceDto
        {
            Parts = new() { new() { TenantId = t1.Id }, new() { TenantId = 9999 } }  // 9999 không tồn tại
        }));
    }

    [Fact]
    public async Task Split_EmptyParts_Throws()
    {
        using var db = new TestDb();
        var t1 = db.AddUser("t1@x.com");
        var inv = SeedInvoice(db, 1_000_000m, t1.Id);

        await Assert.ThrowsAsync<AppException>(() =>
            Make(db).SplitAsync(inv.Id, new SplitInvoiceDto { Parts = new() }));
    }
}
