namespace LogisticsScheduler.API.DTOs;

public class FeedbackCreateDto
{
    public int JobId { get; set; }
    public int Timeliness { get; set; }
    public int ProductCondition { get; set; }
    public int StaffBehaviour { get; set; }
    public string? Comments { get; set; }
}
