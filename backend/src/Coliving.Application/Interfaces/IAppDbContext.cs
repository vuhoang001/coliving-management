using Coliving.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Coliving.Application.Interfaces;

/// <summary>Trừu tượng hoá DbContext để tầng Application không phụ thuộc Infrastructure.</summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Notification> Notifications { get; }

    DbSet<Building> Buildings { get; }
    DbSet<Apartment> Apartments { get; }
    DbSet<Room> Rooms { get; }
    DbSet<Asset> Assets { get; }

    DbSet<ServiceCatalog> ServiceCatalogs { get; }
    DbSet<Amenity> Amenities { get; }

    DbSet<Booking> Bookings { get; }
    DbSet<Contract> Contracts { get; }
    DbSet<CheckRecord> CheckRecords { get; }

    DbSet<Incident> Incidents { get; }
    DbSet<AmenityBooking> AmenityBookings { get; }
    DbSet<ServiceRequest> ServiceRequests { get; }

    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceItem> InvoiceItems { get; }
    DbSet<InvoiceShare> InvoiceShares { get; }
    DbSet<Payment> Payments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
