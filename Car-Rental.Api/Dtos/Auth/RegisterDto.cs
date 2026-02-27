namespace Car_Rental.Api.Dtos.Auth;

public class RegisterDto
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string Role { get; set; } = "Customer"; // Customer or Admin
}