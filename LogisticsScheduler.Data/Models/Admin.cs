namespace LogisticsScheduler.Data.Models
{
    public class Admin : User
    {
        public int AdminId { get; set; }

        // The 'Username' and 'PasswordHash' properties are now inherited from the User class.

        public override string Role => "Admin";
    }
}