using Car_Rental.Api.Data;
using Car_Rental.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Car_Rental.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VehiclesController : ControllerBase
{
    private readonly AppDbContext _context;

    public VehiclesController(AppDbContext context)
    {
        _context = context;
    }

    // Public: anyone can search available cars
    [AllowAnonymous]
    [HttpGet("available")]
    public async Task<ActionResult<List<Vehicle>>> GetAvailable(
        [FromQuery] DateTime start,
        [FromQuery] DateTime end,
        [FromQuery] string? category = null)
    {
        if (end <= start)
            return BadRequest("end must be after start.");

        if (start.Kind == DateTimeKind.Unspecified)
            start = DateTime.SpecifyKind(start, DateTimeKind.Utc);

        if (end.Kind == DateTimeKind.Unspecified)
            end = DateTime.SpecifyKind(end, DateTimeKind.Utc);

        var bookedVehicleIds = await _context.Bookings
            .Where(b => b.Status == "Active"
                        && start < b.EndDate
                        && end > b.StartDate)
            .Select(b => b.VehicleId)
            .Distinct()
            .ToListAsync();

        var maintenanceVehicleIds = await _context.Maintenances
            .Where(m => m.Status == "Active"
                        && start < m.EndDate
                        && end > m.StartDate)
            .Select(m => m.VehicleId)
            .Distinct()
            .ToListAsync();

        var query = _context.Vehicles.AsQueryable()
            .Where(v => v.Status == "Available");

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(v => v.Category == category);

        var available = await query
            .Where(v => !bookedVehicleIds.Contains(v.Id)
                        && !maintenanceVehicleIds.Contains(v.Id))
            .ToListAsync();

        return Ok(available);
    }

    // Admin only
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<List<Vehicle>>> GetAll()
        => await _context.Vehicles.ToListAsync();

    // Admin only
    [Authorize(Roles = "Admin")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Vehicle>> GetById(int id)
    {
        var v = await _context.Vehicles.FindAsync(id);
        return v == null ? NotFound() : v;
    }

    // Admin only
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<Vehicle>> Create(Vehicle vehicle)
    {
        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = vehicle.Id }, vehicle);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]

    public async Task<ActionResult<Vehicle>> Update(int id, Vehicle vehicle)
    {
        var existing = await _context.Vehicles.FindAsync(id);
        if (existing == null) return NotFound();

        existing.Make = vehicle.Make;
        existing.Model = vehicle.Model;
        existing.Year = vehicle.Year;
        existing.Category = vehicle.Category;
        existing.Color = vehicle.Color;
        existing.Seats = vehicle.Seats;
        existing.Mileage = vehicle.Mileage;
        existing.Status = vehicle.Status;

        await _context.SaveChangesAsync();
        return Ok(existing);

    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]

    public async Task<ActionResult> Delete(int id)
    {
        var vehicle = await _context.Vehicles.FindAsync();
        if (vehicle == null) return NotFound();

        var hasActiveBooking = await _context.Bookings.AnyAsync(b => b.VehicleId == id && b.Status == "Active");
        if (hasActiveBooking)
             return BadRequest("Cannot delete a vehicle with active bookings.");

        _context.Vehicles.Remove(vehicle);
        await _context.SaveChangesAsync();
        return NoContent();



    }

    }