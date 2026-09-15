using Coliving.Application.Interfaces;
using Coliving.Domain.Common;
using Coliving.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Coliving.Infrastructure.Persistence;

/// <summary>EF Core DbContext — hiện thực IAppDbContext. Schema tạo bằng EnsureCreated lúc khởi động.</summary>
public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<Apartment> Apartments => Set<Apartment>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Asset> Assets => Set<Asset>();

    public DbSet<ServiceCatalog> ServiceCatalogs => Set<ServiceCatalog>();
    public DbSet<Amenity> Amenities => Set<Amenity>();

    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<CheckRecord> CheckRecords => Set<CheckRecord>();

    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<AmenityBooking> AmenityBookings => Set<AmenityBooking>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();

    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<InvoiceShare> InvoiceShares => Set<InvoiceShare>();
    public DbSet<Payment> Payments => Set<Payment>();

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => await Database.BeginTransactionAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // ---- Index & ràng buộc duy nhất ----
        b.Entity<User>().HasIndex(x => x.Email).IsUnique();
        b.Entity<Booking>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Contract>().HasIndex(x => x.ContractNumber).IsUnique();
        b.Entity<Incident>().HasIndex(x => x.Code).IsUnique();
        b.Entity<ServiceRequest>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Invoice>().HasIndex(x => x.InvoiceNumber).IsUnique();
        b.Entity<Room>().HasIndex(x => x.Code).IsUnique();

        // ---- Precision cho tiền tệ (numeric 18,2) ----
        foreach (var p in b.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            p.SetPrecision(18);
            p.SetScale(2);
        }

        // ---- Quan hệ chính ----
        b.Entity<Apartment>().HasOne(a => a.Building).WithMany(bd => bd.Apartments)
            .HasForeignKey(a => a.BuildingId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Room>().HasOne(r => r.Apartment).WithMany(a => a.Rooms)
            .HasForeignKey(r => r.ApartmentId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<Asset>().HasOne(a => a.Building).WithMany(bd => bd.Assets)
            .HasForeignKey(a => a.BuildingId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<Asset>().HasOne(a => a.Apartment).WithMany()
            .HasForeignKey(a => a.ApartmentId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<Asset>().HasOne(a => a.Room).WithMany(r => r.Assets)
            .HasForeignKey(a => a.RoomId).OnDelete(DeleteBehavior.SetNull);

        b.Entity<Amenity>().HasOne(a => a.Building).WithMany(bd => bd.Amenities)
            .HasForeignKey(a => a.BuildingId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<Booking>().HasOne(bk => bk.Room).WithMany(r => r.Bookings)
            .HasForeignKey(bk => bk.RoomId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Booking>().HasOne(bk => bk.Tenant).WithMany(u => u.Bookings)
            .HasForeignKey(bk => bk.TenantId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<Contract>().HasOne(c => c.Booking).WithOne(bk => bk.Contract)
            .HasForeignKey<Contract>(c => c.BookingId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Contract>().HasOne(c => c.Tenant).WithMany()
            .HasForeignKey(c => c.TenantId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<CheckRecord>().HasOne(c => c.Booking).WithMany(bk => bk.CheckRecords)
            .HasForeignKey(c => c.BookingId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<CheckRecord>().HasOne(c => c.Staff).WithMany()
            .HasForeignKey(c => c.StaffId).OnDelete(DeleteBehavior.SetNull);

        b.Entity<Incident>().HasOne(i => i.Reporter).WithMany()
            .HasForeignKey(i => i.ReporterId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Incident>().HasOne(i => i.AssignedTo).WithMany()
            .HasForeignKey(i => i.AssignedToId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<Incident>().HasOne(i => i.Building).WithMany()
            .HasForeignKey(i => i.BuildingId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<Incident>().HasOne(i => i.Room).WithMany()
            .HasForeignKey(i => i.RoomId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<Incident>().HasOne(i => i.Asset).WithMany()
            .HasForeignKey(i => i.AssetId).OnDelete(DeleteBehavior.SetNull);

        b.Entity<AmenityBooking>().HasOne(ab => ab.Amenity).WithMany(a => a.Bookings)
            .HasForeignKey(ab => ab.AmenityId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<AmenityBooking>().HasOne(ab => ab.User).WithMany()
            .HasForeignKey(ab => ab.UserId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<ServiceRequest>().HasOne(s => s.Service).WithMany(sc => sc.Requests)
            .HasForeignKey(s => s.ServiceCatalogId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ServiceRequest>().HasOne(s => s.Requester).WithMany()
            .HasForeignKey(s => s.RequesterId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ServiceRequest>().HasOne(s => s.Room).WithMany()
            .HasForeignKey(s => s.RoomId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<ServiceRequest>().HasOne(s => s.AssignedTo).WithMany()
            .HasForeignKey(s => s.AssignedToId).OnDelete(DeleteBehavior.SetNull);

        b.Entity<Invoice>().HasOne(i => i.Tenant).WithMany(u => u.Invoices)
            .HasForeignKey(i => i.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Invoice>().HasOne(i => i.Room).WithMany()
            .HasForeignKey(i => i.RoomId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<InvoiceItem>().HasOne(it => it.Invoice).WithMany(i => i.Items)
            .HasForeignKey(it => it.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<InvoiceShare>().HasOne(s => s.Invoice).WithMany(i => i.Shares)
            .HasForeignKey(s => s.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<InvoiceShare>().HasOne(s => s.Tenant).WithMany()
            .HasForeignKey(s => s.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Payment>().HasOne(p => p.Invoice).WithMany(i => i.Payments)
            .HasForeignKey(p => p.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Payment>().HasOne(p => p.PaidBy).WithMany()
            .HasForeignKey(p => p.PaidById).OnDelete(DeleteBehavior.Restrict);

        // ---- Lọc mềm toàn cục: chỉ lấy bản ghi chưa xoá ----
        foreach (var et in b.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(et.ClrType))
            {
                var method = typeof(AppDbContext)
                    .GetMethod(nameof(SetSoftDeleteFilter),
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                    .MakeGenericMethod(et.ClrType);
                method.Invoke(null, new object[] { b });
            }
        }
    }

    private static void SetSoftDeleteFilter<TEntity>(ModelBuilder b) where TEntity : BaseEntity
        => b.Entity<TEntity>().HasQueryFilter(e => e.DeletedAt == null);

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
