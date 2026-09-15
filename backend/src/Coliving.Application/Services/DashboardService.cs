using Coliving.Application.DTOs;
using Coliving.Application.Interfaces;
using Coliving.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Coliving.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IAppDbContext _db;
    public DashboardService(IAppDbContext db) => _db = db;

    public async Task<DashboardStatsDto> GetStatsAsync()
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var totalRooms = await _db.Rooms.CountAsync();
        var occupied = await _db.Rooms.CountAsync(r => r.Status == RoomStatus.Occupied);
        var available = await _db.Rooms.CountAsync(r => r.Status == RoomStatus.Available);

        var revenueThisMonth = await _db.Payments
            .Where(p => p.Status == PaymentStatus.Paid && p.PaidAt >= monthStart)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        var outstanding = await _db.Invoices
            .Where(i => i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid
                        || i.Status == InvoiceStatus.Overdue)
            .SumAsync(i => (decimal?)(i.Total - i.PaidAmount)) ?? 0m;

        return new DashboardStatsDto
        {
            Buildings = await _db.Buildings.CountAsync(),
            Rooms = totalRooms,
            OccupiedRooms = occupied,
            AvailableRooms = available,
            OccupancyRate = totalRooms == 0 ? 0 : Math.Round(occupied * 100.0 / totalRooms, 1),
            ActiveTenants = await _db.Users.CountAsync(u => u.Role == UserRole.Tenant && u.IsActive),
            ActiveContracts = await _db.Contracts.CountAsync(c => c.Status == ContractStatus.Active),
            OpenIncidents = await _db.Incidents.CountAsync(i =>
                i.Status == IncidentStatus.Open || i.Status == IncidentStatus.InProgress),
            PendingBookings = await _db.Bookings.CountAsync(b => b.Status == BookingStatus.Pending),
            RevenueThisMonth = revenueThisMonth,
            OutstandingAmount = outstanding,
            OverdueInvoices = await _db.Invoices.CountAsync(i =>
                i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled && i.DueDate < now)
        };
    }

    public async Task<RevenueReportDto> GetRevenueAsync(int months)
    {
        months = Math.Clamp(months, 1, 24);
        var now = DateTime.UtcNow;
        var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-(months - 1));

        var payments = await _db.Payments.AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Paid && p.PaidAt >= start)
            .Select(p => new { p.PaidAt, p.Amount })
            .ToListAsync();

        var invoices = await _db.Invoices.AsNoTracking()
            .Where(i => i.Status != InvoiceStatus.Cancelled && i.IssueDate >= start)
            .Select(i => new { i.IssueDate, i.Total })
            .ToListAsync();

        var points = new List<RevenuePointDto>();
        for (var m = 0; m < months; m++)
        {
            var month = start.AddMonths(m);
            var key = month.ToString("yyyy-MM");
            var revenue = payments.Where(p => p.PaidAt is { } d && d.Year == month.Year && d.Month == month.Month)
                .Sum(p => p.Amount);
            var expected = invoices.Where(i => i.IssueDate.Year == month.Year && i.IssueDate.Month == month.Month)
                .Sum(i => i.Total);
            points.Add(new RevenuePointDto { Month = key, Revenue = revenue, Expected = expected });
        }

        return new RevenueReportDto { Points = points, Total = points.Sum(p => p.Revenue) };
    }

    public async Task<OccupancyReportDto> GetOccupancyAsync()
    {
        var buildings = await _db.Buildings.AsNoTracking()
            .Select(b => new
            {
                b.Id, b.Name,
                Total = b.Apartments.SelectMany(a => a.Rooms).Count(),
                Occupied = b.Apartments.SelectMany(a => a.Rooms).Count(r => r.Status == RoomStatus.Occupied)
            })
            .ToListAsync();

        var list = buildings.Select(b => new OccupancyByBuildingDto
        {
            BuildingId = b.Id, BuildingName = b.Name,
            TotalRooms = b.Total, OccupiedRooms = b.Occupied,
            OccupancyRate = b.Total == 0 ? 0 : Math.Round(b.Occupied * 100.0 / b.Total, 1)
        }).ToList();

        var totalRooms = list.Sum(b => b.TotalRooms);
        var totalOccupied = list.Sum(b => b.OccupiedRooms);
        return new OccupancyReportDto
        {
            Buildings = list,
            OverallRate = totalRooms == 0 ? 0 : Math.Round(totalOccupied * 100.0 / totalRooms, 1)
        };
    }
}
