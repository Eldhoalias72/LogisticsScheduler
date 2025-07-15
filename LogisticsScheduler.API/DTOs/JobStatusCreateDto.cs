namespace LogisticsScheduler.API.DTOs;

public class JobStatusCreateDto
{
    public int JobId { get; set; }
    public string Status { get; set; }
    public DateTime TimeStamp { get; set; }
}
