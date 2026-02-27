using Car_Rental.Api.Data;
using Car_Rental.Api.Models;
using Car_Rental.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Car_Rental.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly AppDbContext _context;

    public BookingsController(AppDbContext context)
    {
        _context = context;
    }
    [Authorize(Roles = "Customer")]
    [HttpPatch("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var booking = await _context.Bookings.FindAsync(id);
        if (booking == null) return NotFound();

        if (booking.UserId != userId)
            return Forbid();

        if (booking.Status == "Completed")
            return BadRequest("Cannot cancel a completed booking.");

        booking.Status = "Cancelled";
        await _context.SaveChangesAsync();

        return Ok(booking);
    }
    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:int}/complete")]
    public async Task<IActionResult> Complete(int id)
    {
        var booking = await _context.Bookings.FindAsync(id);
        if (booking == null) return NotFound();

        if (booking.Status == "Cancelled")
            return BadRequest("Cannot complete a cancelled booking.");

        booking.Status = "Completed";
        await _context.SaveChangesAsync();

        return Ok(booking);
    }

    [Authorize(Roles = "Customer,Admin")]
    [HttpGet("my")]
    public async Task<IActionResult> MyBookings()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        // Email comes from the JWT (we put it in token claims)
        var email = User.FindFirstValue(ClaimTypes.Name);

        var myBookings = await _context.Bookings
            .Where(b => b.UserId == userId)
            .Include(b => b.Vehicle)
            .OrderByDescending(b => b.StartDate)
            .Select(b => new
            {
                b.Id,
                b.VehicleId,
                Vehicle = b.Vehicle == null ? null : new
                {
                    b.Vehicle.Id,
                    b.Vehicle.Make,
                    b.Vehicle.Model,
                    b.Vehicle.Year,
                    b.Vehicle.Category,
                    b.Vehicle.Color,
                    b.Vehicle.Seats,
                    b.Vehicle.Mileage,
                    b.Vehicle.Status
                },
                b.StartDate,
                b.EndDate,
                b.Status
            })
            .ToListAsync();

        return Ok(new
        {
            user = new { id = userId, email },
            bookings = myBookings
        });
    }
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<List<Booking>>> GetAll()
        => await _context.Bookings.Include(b => b.Vehicle).ToListAsync();

    [Authorize(Roles = "Customer")]
    [HttpPost]
    public async Task<ActionResult> Create(CreateBookingDto dto)
    {
        if (dto.EndDate <= dto.StartDate)
            return BadRequest("EndDate must be after StartDate.");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized("No user id found in token.");

        // Make incoming dates UTC if needed
        var start = dto.StartDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dto.StartDate, DateTimeKind.Utc)
            : dto.StartDate;

        var end = dto.EndDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dto.EndDate, DateTimeKind.Utc)
            : dto.EndDate;

        var vehicleExists = await _context.Vehicles.AnyAsync(v => v.Id == dto.VehicleId);
        if (!vehicleExists) return BadRequest("VehicleId does not exist.");

        var overlaps = await _context.Bookings.AnyAsync(b =>
            b.VehicleId == dto.VehicleId &&
            b.Status == "Active" &&
            start < b.EndDate &&
            end > b.StartDate
        );

        if (overlaps)
            return Conflict("Vehicle is already booked for the selected dates.");

        var booking = new Booking
        {
            VehicleId = dto.VehicleId,
            
            StartDate = start,
            EndDate = end,
            Status = "Active", // ✅ set here, not from client
            UserId = userId
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        return Ok(booking);


    }
}