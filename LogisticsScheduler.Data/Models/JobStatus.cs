using System.ComponentModel.DataAnnotations;

namespace LogisticsScheduler.Data.Models
{
    public class JobStatus
    {
        [Key]
        public int UpdateId { get; set; }
        public int JobId { get; set; }
        public string Status { get; set; }
        public DateTime TimeStamp { get; set; }

        public Job? Job { get; set; }
    }
}
