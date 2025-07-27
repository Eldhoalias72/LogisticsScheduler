namespace LogisticsScheduler.API.DTOs;

public class JobCreateDto
{
    public int? DriverId { get; set; }
    public string PickupAddress { get; set; }
    public string DeliveryAddress { get; set; }
    public int Priority { get; set; }
    public string Status { get; set; }
    public DateTime ScheduledTime { get; set; }
    public string CustomerName { get; set; }
    public string CustomerEmail { get; set; }
    public string CustomerNumber { get; set; }
}