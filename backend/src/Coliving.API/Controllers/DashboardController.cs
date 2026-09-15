using Coliving.Application.DTOs;
using Coliving.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coliving.API.Controllers;

[Authorize(Roles = "Manager,Admin,Staff")]
public class DashboardController : BaseApiController
{
    private readonly IDashboardService _service;
    public DashboardController(IDashboardService service) => _service = service;

    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsDto>> Stats() => Ok(await _service.GetStatsAsync());

    [HttpGet("revenue")]
    public async Task<ActionResult<RevenueReportDto>> Revenue([FromQuery] int months = 6)
        => Ok(await _service.GetRevenueAsync(months));

    [HttpGet("occupancy")]
    public async Task<ActionResult<OccupancyReportDto>> Occupancy() => Ok(await _service.GetOccupancyAsync());
}
