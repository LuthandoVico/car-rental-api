using Car_Rental.Api.Data;
using Car_Rental.Api.Dtos;
using Car_Rental.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Car_Rental.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MaintenanceController : ControllerBase
{
    private readonly AppDbContext _context;

    public MaintenanceController(AppDbContext context)
    {
        _context = context;
    }
    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:int}/complete")]
    public async Task<IActionResult> Complete(int id)
    {
        var m = await _context.Maintenances.FindAsync(id);
        if (m == null) return NotFound();

        m.Status = "Completed";
        await _context.SaveChangesAsync();

        return Ok(m);
    }

    [Authorize(Roles = "Admin")]    
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var m = await _context.Maintenances.FindAsync(id);
        if (m == null) return NotFound();

        _context.Maintenances.Remove(m);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet]
    public async Task<ActionResult<List<Maintenance>>> GetAll()
        => await _context.Maintenances.Include(m => m.Vehicle).ToListAsync();

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<Maintenance>> Create(CreateMaintenanceDto dto)
    {
        if (dto.EndDate <= dto.StartDate)
            return BadRequest("EndDate must be after StartDate.");

        // Make dates UTC if needed
        var start = dto.StartDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dto.StartDate, DateTimeKind.Utc)
            : dto.StartDate;

        var end = dto.EndDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dto.EndDate, DateTimeKind.Utc)
            : dto.EndDate;

        var vehicleExists = await _context.Vehicles.AnyAsync(v => v.Id == dto.VehicleId);
        if (!vehicleExists) return BadRequest("VehicleId does not exist.");

        // Block maintenance if it overlaps an ACTIVE booking
        var overlapsBooking = await _context.Bookings.AnyAsync(b =>
            b.VehicleId == dto.VehicleId &&
            b.Status == "Active" &&
            start < b.EndDate &&
            end > b.StartDate
        );
        if (overlapsBooking)
            return Conflict("Cannot schedule maintenance: vehicle has an active booking during these dates.");

        // Prevent overlapping maintenance too
        var overlapsMaintenance = await _context.Maintenances.AnyAsync(m =>
            m.VehicleId == dto.VehicleId &&
            start < m.EndDate &&
            end > m.StartDate
        );
        if (overlapsMaintenance)
            return Conflict("Maintenance already exists for this vehicle in the selected dates.");

        var maintenance = new Maintenance
        {
            VehicleId = dto.VehicleId,
            StartDate = start,
            EndDate = end,
            Notes = dto.Notes
        };

        _context.Maintenances.Add(maintenance);
        await _context.SaveChangesAsync();

        return Ok(maintenance);
    }
}