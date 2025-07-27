using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogisticsScheduler.Data.Models
{
    public abstract class User
    {
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public abstract string Role { get; }
    }
}