namespace LogisticsScheduler.Data.Models
{
    public class Job
    {
        public int JobId { get; set; }
        public int? DriverId { get; set; }
        public string PickupAddress { get; set; }
        public string DeliveryAddress { get; set; }
        public int Priority { get; set; }
        public string Status { get; set; }
        public DateTime ScheduledTime { get; set; }
        public string CustomerName { get; set; }            
        public string CustomerEmail { get; set; }           
        public string CustomerNumber { get; set; }

        public Driver? Driver { get; set; }
        public ICollection<JobStatus>? JobStatuses { get; set; }
        public Feedback? Feedback { get; set; }
    }
}