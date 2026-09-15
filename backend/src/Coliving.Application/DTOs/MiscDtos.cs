namespace Coliving.Application.DTOs;

public record NotificationDto
{
    public int Id { get; init; }
    public string Title { get; init; } = default!;
    public string Message { get; init; } = default!;
    public string Type { get; init; } = default!;
    public string? Link { get; init; }
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record NotificationListDto
{
    public List<NotificationDto> Items { get; init; } = new();
    public int UnreadCount { get; init; }
}

// ---------- Dashboard & báo cáo ----------

public record DashboardStatsDto
{
    public int Buildings { get; init; }
    public int Rooms { get; init; }
    public int OccupiedRooms { get; init; }
    public int AvailableRooms { get; init; }
    public double OccupancyRate { get; init; }
    public int ActiveTenants { get; init; }
    public int ActiveContracts { get; init; }
    public int OpenIncidents { get; init; }
    public int PendingBookings { get; init; }
    public decimal RevenueThisMonth { get; init; }
    public decimal OutstandingAmount { get; init; }
    public int OverdueInvoices { get; init; }
}

public record RevenuePointDto
{
    public string Month { get; init; } = default!;  // yyyy-MM
    public decimal Revenue { get; init; }
    public decimal Expected { get; init; }
}

public record RevenueReportDto
{
    public List<RevenuePointDto> Points { get; init; } = new();
    public decimal Total { get; init; }
}

public record OccupancyByBuildingDto
{
    public int BuildingId { get; init; }
    public string BuildingName { get; init; } = default!;
    public int TotalRooms { get; init; }
    public int OccupiedRooms { get; init; }
    public double OccupancyRate { get; init; }
}

public record OccupancyReportDto
{
    public List<OccupancyByBuildingDto> Buildings { get; init; } = new();
    public double OverallRate { get; init; }
}
