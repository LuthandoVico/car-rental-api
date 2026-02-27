namespace Car_Rental.Api.Models;

public class Booking
{
    public int Id { get; set; }

    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public string CustomerName { get; set; } = "";

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public string Status { get; set; } = "Active"; // Active, Cancelled, Completed

    public string UserId { get; set; } = "";

    // public AppUser? User { get; set; }
}