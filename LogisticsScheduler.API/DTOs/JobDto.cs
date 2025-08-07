// In LogisticsScheduler.API/DTOs/JobDto.cs
namespace LogisticsScheduler.API.DTOs
{
    public class JobDto
    {
        public int JobId { get; set; }
        public string PickupAddress { get; set; }
        public string DeliveryAddress { get; set; }
        public int Priority { get; set; }
        public string Status { get; set; }
        public DateTime ScheduledTime { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerNumber { get; set; }

        // This will hold simplified driver info, breaking the cycle
        public DriverDto? Driver { get; set; }
    }
}