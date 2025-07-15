namespace LogisticsScheduler.Data.Models
{
    public class Feedback
    {
        public int FeedbackId { get; set; }
        public int JobId { get; set; }
        public int Timeliness { get; set; }
        public int ProductCondition { get; set; }
        public int StaffBehaviour { get; set; }
        public string? Comments { get; set; }

        public Job? Job { get; set; }
    }
}
