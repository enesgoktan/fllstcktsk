using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using kebapbackend.Data;
using kebapbackend.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace kebapbackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Ana sayfa için authentication gerekli
    public class HomeController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        // Mevcut kullanıcı bilgilerini döndür (AuthController'daki ile tutarlı)
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var username = User.Identity?.Name;

            if (string.IsNullOrEmpty(username))
                return Unauthorized("Kullanıcı oturumu bulunamadı.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
                return NotFound("Kullanıcı bulunamadı.");

            return Ok(new
            {
                Id = user.Id,
                Username = user.Username,
                Name = user.Name,
                Email = user.Email,
                Position = user.Position,
                IsAdmin = user.Position == "admin",
                MustChangePassword = user.MustChangePassword,
                CreatedAt = user.CreatedAt
            });
        }

        // Ana sayfa istatistikleri
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardData()
        {
            try
            {
                var totalEmployees = await _context.Employees.CountAsync();
                var totalUsers = await _context.Users.CountAsync();
                var managersCount = await _context.Employees
                    .Where(e => e.Position.ToLower().Contains("manager"))
                    .CountAsync();

                // Son eklenen çalışanlar (5 tanesi)
                var recentEmployees = await _context.Employees
                    .OrderByDescending(e => e.Id)
                    .Take(5)
                    .ToListAsync();

                return Ok(new
                {
                    TotalEmployees = totalEmployees,
                    TotalUsers = totalUsers,
                    ManagersCount = managersCount,
                    RecentEmployees = recentEmployees
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Dashboard verileri yüklenirken hata oluştu",
                    error = ex.Message
                });
            }
        }

        // Pozisyonu manager olan çalışanlar listesi
        [HttpGet("managers")]
        public async Task<IActionResult> GetManagers()
        {
            try
            {
                var managers = await _context.Employees
                    .Where(e => e.Position.ToLower().Contains("manager"))
                    .Select(e => new {
                        e.Id,
                        e.Name,
                        e.Position,
                        e.Age
                    })
                    .ToListAsync();

                return Ok(managers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Manager listesi yüklenirken hata oluştu",
                    error = ex.Message
                });
            }
        }

        // Çalışan sayısını verir
        [HttpGet("employee-count")]
        public async Task<IActionResult> GetEmployeeCount()
        {
            try
            {
                var count = await _context.Employees.CountAsync();
                return Ok(new { employeeCount = count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Çalışan sayısı alınırken hata oluştu",
                    error = ex.Message
                });
            }
        }

        // Test endpoint - Authentication kontrolü için
        [HttpGet("test")]
        public IActionResult TestEndpoint()
        {
            var username = User.Identity?.Name;
            var isAuthenticated = User.Identity?.IsAuthenticated ?? false;

            return Ok(new
            {
                message = "Home controller çalışıyor!",
                username = username,
                isAuthenticated = isAuthenticated,
                timestamp = DateTime.Now
            });
        }

        // Admin kontrolü gerektiren endpoint
        [HttpGet("admin-stats")]
        public async Task<IActionResult> GetAdminStats()
        {
            // Position kontrolü
            var userPosition = User.Claims.FirstOrDefault(c => c.Type == "position")?.Value;

            if (userPosition != "admin")
                return Forbid("Bu endpoint sadece admin kullanıcıları için.");

            try
            {
                var stats = new
                {
                    TotalEmployees = await _context.Employees.CountAsync(),
                    TotalUsers = await _context.Users.CountAsync(),
                    UsersRequiringPasswordChange = await _context.Users.CountAsync(u => u.MustChangePassword),
                    AdminUsers = await _context.Users.CountAsync(u => u.Position == "admin")
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Admin istatistikleri yüklenirken hata oluştu",
                    error = ex.Message
                });
            }
        }
    }
}