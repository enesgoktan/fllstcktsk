using Microsoft.AspNetCore.Mvc;
using kebapbackend.Data;
using kebapbackend.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace kebapbackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(username))
            {
                return Unauthorized("Kimlik doğrulaması yok veya kullanıcı adı claim'i bulunamadı.");
            }

            var user = await _context.Users
                .Where(u => u.Username == username)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.Name,
                    u.Position,
                    u.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }

            return Ok(user);
        }

        // 2. Pozisyonu manager olan çalışanlar listesi
        [HttpGet("managers")]
        public async Task<IActionResult> GetManagers()
        {
            var managers = await _context.Employees
                .Where(e => e.Position.ToLower().Contains("manager"))
                .ToListAsync();

            return Ok(managers);
        }

        // 3. Çalışan sayısını verir
        [HttpGet("employee-count")]
        public async Task<IActionResult> GetEmployeeCount()
        {
            var count = await _context.Employees.CountAsync();
            return Ok(new { employeeCount = count });
        }
    }
}
