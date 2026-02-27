namespace Car_Rental.Api.Dtos;

public class CreateMaintenanceDto
{
    public int VehicleId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Notes { get; set; } = "";
}