using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogisticsScheduler.Data.Models
{
   
        public class Admin
        {
            public int AdminId { get; set; }
            public string Username { get; set; }
            public string PasswordHash { get; set; }
        }

   
}
