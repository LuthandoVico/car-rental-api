namespace Car_Rental.Api.Models;

public class Vehicle
{
    public int Id { get; set; }
    public string Make { get; set; } = "";
    public string Model { get; set; } = "";
    public int Year { get; set; }
    public string Category { get; set; } = "";
    public string Color { get; set; } = "";
    public int Seats { get; set; }
    public int Mileage { get; set; }
    public string Status { get; set; } = "Available";
}