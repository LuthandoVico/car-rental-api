using Car_Rental.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Car_Rental.Api.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = "Admin")]
public class AdminDashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminDashboardController(AppDbContext context)
    {
        _context = context;
    }

    // GET /api/admin/dashboard/stats
    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
    {
        var totalVehicles = await _context.Vehicles.CountAsync();
        var totalBookings = await _context.Bookings.CountAsync();

        var activeBookings = await _context.Bookings.CountAsync(b => b.Status == "Active");
        var cancelledBookings = await _context.Bookings.CountAsync(b => b.Status == "Cancelled");
        var completedBookings = await _context.Bookings.CountAsync(b => b.Status == "Completed");

        var activeMaintenance = await _context.Maintenances.CountAsync(m => m.Status == "Active");

        return Ok(new
        {
            totalVehicles,
            totalBookings,
            bookings = new { active = activeBookings, cancelled = cancelledBookings, completed = completedBookings },
            activeMaintenance
        });
    }

    // GET /api/admin/dashboard/revenue?from=2026-03-01&to=2026-03-31&dailyRate=500
    // Simple revenue estimate: completed booking days * dailyRate
    [HttpGet("revenue")]
    public async Task<IActionResult> Revenue(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] decimal dailyRate = 500m)
    {
        if (to <= from) return BadRequest("to must be after from.");

        var bookings = await _context.Bookings
            .Where(b => b.Status == "Completed"
                        && b.StartDate < to
                        && b.EndDate > from)
            .Select(b => new { b.StartDate, b.EndDate })
            .ToListAsync();

        decimal total = 0m;

        foreach (var b in bookings)
        {
            var start = b.StartDate < from ? from : b.StartDate;
            var end = b.EndDate > to ? to : b.EndDate;

            var days = (end.Date - start.Date).TotalDays;
            if (days < 0) days = 0;

            total += (decimal)days * dailyRate;
        }

        return Ok(new
        {
            from,
            to,
            dailyRate,
            completedBookingsCount = bookings.Count,
            estimatedRevenue = total
        });
    }

    // GET /api/admin/dashboard/vehicle-usage?from=2026-03-01&to=2026-03-31
    // Usage = how many completed booking days per vehicle in range
    [HttpGet("vehicle-usage")]
    public async Task<IActionResult> VehicleUsage([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        if (to <= from) return BadRequest("to must be after from.");

        var data = await _context.Bookings
            .Where(b => b.Status == "Completed"
                        && b.StartDate < to
                        && b.EndDate > from)
            .GroupBy(b => b.VehicleId)
            .Select(g => new
            {
                vehicleId = g.Key,
                bookings = g.Count(),
                // rough: sum of booking days (clamped) in SQL is messy; we’ll compute after
                ranges = g.Select(x => new { x.StartDate, x.EndDate }).ToList()
            })
            .ToListAsync();

        var result = data.Select(x =>
        {
            double days = 0;
            foreach (var r in x.ranges)
            {
                var start = r.StartDate < from ? from : r.StartDate;
                var end = r.EndDate > to ? to : r.EndDate;
                var d = (end.Date - start.Date).TotalDays;
                if (d > 0) days += d;
            }

            return new
            {
                x.vehicleId,
                x.bookings,
                bookedDays = days
            };
        })
        .OrderByDescending(x => x.bookedDays)
        .ToList();

        return Ok(result);
    }
}