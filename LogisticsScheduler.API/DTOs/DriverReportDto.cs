namespace LogisticsScheduler.API.DTOs
{
    public class DriverReportDto
    {
        public int DriverId { get; set; }
        public string DriverName { get; set; }
        public int TotalJobs { get; set; }
        public double AverageTimeliness { get; set; }
        public double AverageProductCondition { get; set; }
        public double AverageStaffBehaviour { get; set; }
    }
}