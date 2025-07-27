using System.Collections.Generic;

namespace LogisticsScheduler.Data.Models
{
    public class Driver : User
    {
        public int DriverId { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public bool IsAvailable { get; set; }
        public int VehicleCapacity { get; set; }

        // The 'Username' and 'PasswordHash' properties are now inherited from the User class.

        public override string Role => "Driver";

        public ICollection<Job>? Jobs { get; set; }
    }
}