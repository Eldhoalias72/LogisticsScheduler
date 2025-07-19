namespace LogisticsScheduler.API.DTOs
{
    public class LoginResponseDto
    {
        public int UserId { get; set; } // Will hold either AdminId or DriverId
        public string Username { get; set; }
        public string Role { get; set; }
    }
}