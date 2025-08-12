using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace kebapbackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet("public")]
        public IActionResult PublicEndpoint()
        {
            return Ok("Bu herkese açık endpoint");
        }

        [Authorize] // Bu JWT token gerektiriyor
        [HttpGet("protected")]
        public IActionResult ProtectedEndpoint()
        {
            var username = User.Identity?.Name;
            return Ok($"Merhaba {username}! Token doğrulandı ✅");
        }
    }
}