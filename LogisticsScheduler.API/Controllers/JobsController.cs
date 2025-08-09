using LogisticsScheduler.API.DTOs;
using LogisticsScheduler.API.Services;
using LogisticsScheduler.Data;
using LogisticsScheduler.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsScheduler.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class JobsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ICacheService _cacheService;
        private const string DashboardCacheKey = "dashboard_stats";

        public JobsController(AppDbContext context, ICacheService cacheService)
        {
            _context = context;
            _cacheService = cacheService;
        }

        // MODIFIED: This is the corrected GET method
        // In LogisticsScheduler.API/Controllers/JobsController.cs
        
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<JobDto>>> GetJobs([FromQuery] DateTime? date)
        {
            var cacheKey = date.HasValue ? $"jobs:{date.Value:yyyy-MM-dd}" : "jobs:all";

            var cachedJobs = await _cacheService.GetData<List<JobDto>>(cacheKey);
            if (cachedJobs != null)
            {
                return Ok(cachedJobs);
            }

            var jobsQuery = _context.Jobs.AsQueryable();

            if (date.HasValue)
            {
                var startDate = date.Value.Date;
                var endDate = startDate.AddDays(1);
                jobsQuery = jobsQuery.Where(j => j.ScheduledTime >= startDate && j.ScheduledTime < endDate);
            }

            // Project the database models into DTOs using Select()
            var jobs = await jobsQuery
                .OrderByDescending(j => j.ScheduledTime)
                .Select(j => new JobDto
                {
                    JobId = j.JobId,
                    PickupAddress = j.PickupAddress,
                    DeliveryAddress = j.DeliveryAddress,
                    Priority = j.Priority,
                    Status = j.Status,
                    ScheduledTime = j.ScheduledTime,
                    CustomerName = j.CustomerName,
                    CustomerEmail = j.CustomerEmail,
                    CustomerNumber = j.CustomerNumber,
                    // Only map the driver fields you need
                    Driver = j.Driver == null ? null : new DriverDto
                    {
                        DriverId = j.Driver.DriverId,
                        Name = j.Driver.Name
                    }
                })
                .ToListAsync();

            await _cacheService.SetData(cacheKey, jobs, TimeSpan.FromMinutes(5));

            return Ok(jobs);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Job>> GetJobById(int id)
        {
            var cacheKey = $"job:{id}";
            var cachedJob = await _cacheService.GetData<Job>(cacheKey);

            if (cachedJob != null)
            {
                return Ok(cachedJob);
            }

            var job = await _context.Jobs
                .Include(j => j.Driver)
                .FirstOrDefaultAsync(j => j.JobId == id);

            if (job == null)
            {
                return NotFound();
            }

            await _cacheService.SetData(cacheKey, job, TimeSpan.FromMinutes(5));

            return Ok(job);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Job>> CreateJob(JobCreateDto dto)
        {
            var job = new Job
            {
                DriverId = dto.DriverId,
                PickupAddress = dto.PickupAddress,
                DeliveryAddress = dto.DeliveryAddress,
                Priority = dto.Priority,
                Status = dto.DriverId.HasValue ? "Assigned" : "Scheduled",
                ScheduledTime = dto.ScheduledTime,
                CustomerName = dto.CustomerName,
                CustomerEmail = dto.CustomerEmail,
                CustomerNumber = dto.CustomerNumber
            };

            _context.Jobs.Add(job);
            await _context.SaveChangesAsync();

            await InvalidateJobCaches(job);

            return CreatedAtAction(nameof(GetJobById), new { id = job.JobId }, job);
        }

        [HttpPost("{id}/auto-assign")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AutoAssignJob(int id, [FromServices] IConfiguration config, [FromServices] ILogger<JobsController> logger)
        {
            logger.LogInformation("Starting auto-assign for job {JobId}", id);

            var job = await _context.Jobs.FindAsync(id);
            if (job == null)
            {
                logger.LogWarning("Job {JobId} not found", id);
                return NotFound("Job not found.");
            }

            if (job.DriverId.HasValue)
            {
                logger.LogWarning("Job {JobId} already assigned to driver {DriverId}", id, job.DriverId);
                return BadRequest("Job is already assigned.");
            }

            var apiKey = config["Geoapify:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                logger.LogError("Geoapify API key is missing");
                return StatusCode(500, "Geoapify API key is missing in configuration.");
            }

            var availableDrivers = await _context.Drivers
                .Where(d => d.IsAvailable)
                .ToListAsync();

            logger.LogInformation("Found {Count} available drivers", availableDrivers.Count);

            if (!availableDrivers.Any())
                return NotFound("No suitable drivers available.");

            // Geocode pickup location once
            var pickupCoords = await GeocodeAddressAsync(job.PickupAddress, apiKey);
            logger.LogInformation("Pickup address: {PickupAddress}, coords: {Coords}", job.PickupAddress, pickupCoords);

            var tasks = availableDrivers.Select(async driver =>
            {
                logger.LogInformation("Processing driver {DriverId} - {DriverName} at {Location}", driver.DriverId, driver.Name, driver.Location);

                var driverCoords = await GeocodeAddressAsync(driver.Location, apiKey);
                logger.LogInformation("Driver {DriverId} coords: {Coords}", driver.DriverId, driverCoords);

                var distance = await CalculateDistanceAsync(driverCoords, pickupCoords, apiKey);
                logger.LogInformation("Distance from driver {DriverId} to pickup: {Distance} meters", driver.DriverId, distance);

                return (Driver: driver, Distance: distance);
            });

            var driverDistances = await Task.WhenAll(tasks);

            var suitableDriver = driverDistances
                .Where(x => x.Distance < double.MaxValue)
                .OrderBy(x => x.Distance)
                .FirstOrDefault().Driver;

            if (suitableDriver != null)
            {
                logger.LogInformation("Assigning driver {DriverId} to job {JobId}", suitableDriver.DriverId, job.JobId);

                job.DriverId = suitableDriver.DriverId;
                job.Status = "Assigned";
                _context.Update(job);
                await _context.SaveChangesAsync();
                await InvalidateJobCaches(job);
                await _context.Entry(job).Reference(j => j.Driver).LoadAsync();

                logger.LogInformation("Job {JobId} successfully assigned", job.JobId);

                return Ok(job);
            }

            logger.LogWarning("No suitable drivers found after distance calculation");
            return NotFound("No suitable drivers available.");
        }


        [HttpPut("{jobId}/reassign/{driverId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReassignJob(int jobId, int driverId)
        {
            var job = await _context.Jobs.FindAsync(jobId);
            if (job == null) return NotFound("Job not found.");

            var driverExists = await _context.Drivers.AnyAsync(d => d.DriverId == driverId);
            if (!driverExists) return BadRequest("Driver not found.");

            job.DriverId = driverId;
            job.Status = "Assigned";
            _context.Update(job);
            await _context.SaveChangesAsync();

            await InvalidateJobCaches(job);

            return NoContent();
        }
        public class JobStatusUpdateDto
        {
            public string Status { get; set; }
        }
        private readonly IEmailService _emailService;

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin,Driver")]
        public async Task<IActionResult> UpdateJobStatus(
        int id,
        [FromBody] JobStatusUpdateDto dto,
        [FromServices] ILogger<JobsController> logger,
        [FromServices] IEmailService emailService)
            {
                logger.LogInformation("Received request to update status for JobId {JobId}", id);

                if (dto == null || string.IsNullOrWhiteSpace(dto.Status))
                    return BadRequest("Status is required");

                var job = await _context.Jobs.FindAsync(id);
                if (job == null)
                    return NotFound();

                logger.LogInformation("Updating job {JobId} from {OldStatus} to {NewStatus}", job.JobId, job.Status, dto.Status);
                job.Status = dto.Status;

                var statusLog = new JobStatus
                {
                    JobId = job.JobId,
                    Status = dto.Status,
                    TimeStamp = DateTime.UtcNow
                };

                _context.JobStatuses.Add(statusLog);
                await _context.SaveChangesAsync();
                await InvalidateJobCaches(job);

                // ✅ Send feedback email if delivered
                if (dto.Status.Equals("Delivered", StringComparison.OrdinalIgnoreCase))
                {
                    var feedbackLink = $"https://localhost:7166/feedback/submit?jobId={job.JobId}";
                    var htmlContent = $@"
                <p>Dear {job.CustomerName},</p>
                <p>Your order has been delivered successfully.</p>
                <p>Please help us improve by providing your feedback:</p>
                <p><a href='{feedbackLink}'>Submit Feedback</a></p>
                <p>Thank you for using Logistics Scheduler!</p>
            ";

                try
                {
                    await emailService.SendEmailAsync(job.CustomerEmail, "Delivery Completed - Feedback Request", htmlContent);
                    logger.LogInformation("Feedback email sent to {Email}", job.CustomerEmail);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send feedback email to {Email}", job.CustomerEmail);
                }
            }

            return NoContent();
        }





        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteJob(int id)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job == null) return NotFound();

            await InvalidateJobCaches(job);

            _context.Jobs.Remove(job);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task InvalidateJobCaches(Job job)
        {
            var tasks = new List<Task>
            {
                _cacheService.RemoveData($"job:{job.JobId}"),
                _cacheService.RemoveData("jobs:all"),
                _cacheService.RemoveData($"jobs:{job.ScheduledTime:yyyy-MM-dd}"),
                _cacheService.RemoveData(DashboardCacheKey)
            };
            await Task.WhenAll(tasks);
        }

        private async Task<double> CalculateDistanceAsync((double Lat, double Lon)? originCoords, (double Lat, double Lon)? destCoords, string apiKey)
        {
            if (originCoords == null || destCoords == null)
            {
                // No valid coordinates — skip routing
                return double.MaxValue;
            }

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var url = $"https://api.geoapify.com/v1/routing?waypoints={originCoords.Value.Lat},{originCoords.Value.Lon}|{destCoords.Value.Lat},{destCoords.Value.Lon}&mode=drive&apiKey={apiKey}";

            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                // Log error but don't crash
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Geoapify routing failed: {error}");
                return double.MaxValue;
            }

            var routeData = await response.Content.ReadFromJsonAsync<DTOs.GeoapifyRouteResponse>();
            return routeData?.features?.FirstOrDefault()?.properties?.distance ?? double.MaxValue;
        }


        private async Task<(double Lat, double Lon)?> GeocodeAddressAsync(string address, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(address)) return null;

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) }; // fail fast
            var url = $"https://api.geoapify.com/v1/geocode/search?text={Uri.EscapeDataString(address)}&apiKey={apiKey}";
            var response = await httpClient.GetFromJsonAsync<DTOs.GeoapifyGeocodeResponse>(url);

            var first = response?.features?.FirstOrDefault();
            if (first == null || first.geometry?.coordinates == null || first.geometry.coordinates.Count < 2)
            {
                Console.WriteLine($"Geocoding failed for address: {address}");
                return null;
            }


            return (first.geometry.coordinates[1], first.geometry.coordinates[0]);
        }




    }
}