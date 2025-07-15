using System.ComponentModel.DataAnnotations.Schema;

namespace LogisticsScheduler.Data.Models
{
    public class Driver
    {
        
        public int DriverId { get; set; }
      
        public string Name { get; set; }

        
        public string Location { get; set; }
     
        public bool IsAvailable { get; set; }
       
        public int VehicleCapacity { get; set; }

        public string Username { get; set; }
        public string PasswordHash { get; set; }


        public ICollection<Job>? Jobs { get; set; }
    }
}
