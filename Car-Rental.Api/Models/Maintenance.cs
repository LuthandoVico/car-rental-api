namespace Car_Rental.Api.Models;

public class Maintenance
{
    public int Id { get; set; }

    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public string Notes { get; set; } = "";

    public string Status { get; set; } = "Active"; // Active, Completed
}