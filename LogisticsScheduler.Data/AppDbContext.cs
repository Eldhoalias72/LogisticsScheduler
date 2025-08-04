using Microsoft.EntityFrameworkCore;
using LogisticsScheduler.Data.Models;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace LogisticsScheduler.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<JobStatus> JobStatuses { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }

        public DbSet<Admin> Admin {get; set; }  

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Job>()
                .HasOne(j => j.Driver)
                .WithMany(d => d.Jobs)
                .HasForeignKey(j => j.DriverId);

            modelBuilder.Entity<JobStatus>()
                .HasOne(js => js.Job)
                .WithMany(j => j.JobStatuses)
                .HasForeignKey(js => js.JobId);

            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.Job)
                .WithOne(j => j.Feedback)
                .HasForeignKey<Feedback>(f => f.JobId);

            modelBuilder.Entity<Admin>().HasData(
                new Admin
                {
                    AdminId = 2, 
                    Username = "admin0",
                  
                    PasswordHash = "$2a$12$xaYILAxGiT.fMakP1yDKne1YwY2dzH0wkJUxcIQtRUCcd4sR8kKDm"
                }
);
        }
    }
}
