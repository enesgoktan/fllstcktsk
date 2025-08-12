using Microsoft.AspNetCore.Mvc;
using kebapbackend.Data;
using kebapbackend.Models;
using Microsoft.EntityFrameworkCore;

namespace kebapbackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TaskassignmentController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TaskassignmentController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var atamalar = await _context.Taskassignments.ToListAsync();
            return Ok(atamalar);
        }

        [HttpPost]
        public async Task<IActionResult> Post(Taskassignment Taskassignment)
        {
            _context.Taskassignments.Add(Taskassignment);
            await _context.SaveChangesAsync();
            return Ok(Taskassignment);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Taskassignments.FindAsync(id);
            if (item == null) return NotFound();
            _context.Taskassignments.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}