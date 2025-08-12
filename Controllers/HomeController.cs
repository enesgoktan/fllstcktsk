using Microsoft.AspNetCore.Mvc;
using kebapbackend.Data;
using kebapbackend.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

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

        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var user = await _context.Users.FirstOrDefaultAsync();
            if (user == null) return NotFound("User not found.");
            return Ok(user);
        }

        // 2. Pozisyonu manager olan çalýþanlar listesi
        [HttpGet("managers")]
        public async Task<IActionResult> GetManagers()
        {
            var managers = await _context.Employees
                .Where(e => e.Position.ToLower().Contains("manager"))
                .ToListAsync();

            return Ok(managers);
        }

        // 3. Çalýþan sayýsýný verir
        [HttpGet("employee-count")]
        public async Task<IActionResult> GetEmployeeCount()
        {
            var count = await _context.Employees.CountAsync();
            return Ok(new { employeeCount = count });
        }
    }
}
