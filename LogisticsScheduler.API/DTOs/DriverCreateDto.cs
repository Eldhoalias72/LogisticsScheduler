namespace LogisticsScheduler.API.DTOs
{
    public class DriverCreateDto
    {
        public string Name { get; set; }
        public string Location { get; set; }
        public bool IsAvailable { get; set; }
        public int VehicleCapacity { get; set; }
        public string Username { get; set; }
        public string Password { get; set; } 
    }
}