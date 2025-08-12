using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using kebapbackend.Data;   
using kebapbackend.Models; 
using System.Linq;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _context;

    public TasksController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult GetAllTasks()
    {
        var tasks = _context.Tasks.ToList(); 
        return Ok(tasks);
    }
}
