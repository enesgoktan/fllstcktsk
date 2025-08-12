using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using kebapbackend.Data;
using kebapbackend.Models;
using Microsoft.EntityFrameworkCore;

namespace kebapbackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly AppDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher = new PasswordHasher<User>();
        public AuthController(IConfiguration config, AppDbContext context)
        {
            _config = config;
            _context = context;

            // Eğer admin kullanıcı yoksa ekle
            InitializeAdmin();
        }

        private void InitializeAdmin()
        {
            if (!_context.Users.Any(u => u.Username == "admin"))
            {
                var admin = new User
                {
                    Username = "admin",
                    Email = "admin@gmail.com",
                    Name = "Admin User",
                    Position = "admin",
                    MustChangePassword = false,
                    CreatedAt = DateTime.UtcNow
                };

                // Şifreyi hashle ve ata
                admin.Password = _passwordHasher.HashPassword(admin, "1234");

                _context.Users.Add(admin);
                _context.SaveChanges();

                Console.WriteLine("✅ Admin kullanıcı oluşturuldu: admin / 1234");
            }
            else
            {


                var existingAdmin = _context.Users.FirstOrDefault(u => u.Username == "admin");
                if (existingAdmin != null && existingAdmin.Position != "admin")
                {
                    existingAdmin.Position = "admin";
                    _context.SaveChanges();
                    Console.WriteLine("✅ Mevcut admin kullanıcısının position'ı 'admin' olarak güncellendi");
                }
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel login)
        {
            Console.WriteLine($"🔐 Login denemesi: {login.Username}");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == login.Username);
            if (user == null)
            {
                Console.WriteLine("❌ Kullanıcı bulunamadı");
                return Unauthorized("Kullanıcı bulunamadı");
            }

            Console.WriteLine($"✅ Kullanıcı bulundu: {user.Name} - Position: {user.Position}");
            Console.WriteLine($"🔑 Gelen şifre: {login.Password}");
            Console.WriteLine($"🔑 TempPassword: {user.TempPassword}");

            // Hashlenmiş şifre doğrulama
            var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.Password, login.Password);
            if (verifyResult == PasswordVerificationResult.Failed)
            {
                Console.WriteLine("❌ Şifre yanlış");
                return Unauthorized("Şifre yanlış");
            }

            Console.WriteLine("✅ Şifre doğru!");

            var token = GenerateToken(user.Username, user.Position);

            if (user.MustChangePassword)
            {
                return Ok(new
                {
                    token,
                    mustChangePassword = true,
                    message = "İlk girişiniz! Şifrenizi değiştirmeniz gerekiyor.",
                    tempPassword = user.TempPassword // Debug için
                });
            }

            return Ok(new { token });
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel model)
        {
            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
                return NotFound("Kullanıcı bulunamadı.");

            // Eski şifre kontrolü
            var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.Password, model.OldPassword);
            if (verifyResult == PasswordVerificationResult.Failed)
                return BadRequest("Eski şifre yanlış.");

            if (string.IsNullOrEmpty(model.NewPassword) || model.NewPassword.Length < 6)
                return BadRequest("Yeni şifre en az 6 karakter olmalı.");

            user.Password = _passwordHasher.HashPassword(user, model.NewPassword);
            user.MustChangePassword = false;
            user.TempPassword = null;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Şifre başarıyla değiştirildi!" });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel register)
        {
            if (register == null)
                return BadRequest("Register verisi boş olamaz.");

            if (string.IsNullOrEmpty(register.Username) || string.IsNullOrEmpty(register.Password))
                return BadRequest("Kullanıcı adı ve şifre gerekli.");

            if (await _context.Users.AnyAsync(u => u.Username == register.Username))
                return BadRequest("Bu kullanıcı adı zaten kullanılıyor.");

            var newUser = new User
            {
                Username = register.Username,
                Email = register.Email ?? "",
                Name = register.Name ?? "",
                Position = register.Position ?? "employee",
                CreatedAt = DateTime.UtcNow
            };

            newUser.Password = _passwordHasher.HashPassword(newUser, register.Password);

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            var token = GenerateToken(newUser.Username, newUser.Position);
            return Ok(new { token, message = "Kayıt başarılı!" });
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            // JWT stateless, frontend'de token silinsin yeter
            return Ok(new { message = "Başarıyla çıkış yapıldı. Token'ı frontend'de silin." });
        }

        // Debug endpoint - Tüm kullanıcıları listele
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users.Select(u => new
            {
                u.Id,
                u.Username,
                u.Name,
                u.Email,
                u.Position,
                u.MustChangePassword,
                u.TempPassword,
                u.CreatedAt
            }).ToListAsync();

            return Ok(users);
        }

        private string GenerateToken(string username, string position = "employee")
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim("position", position)
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            int expireMinutes = int.TryParse(_config["Jwt:ExpireMinutes"], out var m) ? m : 60;

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(expireMinutes),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public class LoginModel
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

        public class RegisterModel
        {
            public string Username { get; set; } = "";
            public string Password { get; set; } = "";
            public string? Email { get; set; } = "";
            public string? Name { get; set; } = "";
            public string? Position { get; set; } = "";
        }

        public class ChangePasswordModel
        {
            public string OldPassword { get; set; } = "";
            public string NewPassword { get; set; } = "";
        }

        [HttpGet("test")]
        public IActionResult TestEndpoint()
        {
            return Ok("Test endpoint çalışıyor!");
        }

        [HttpGet("test-admin")]
        public async Task<IActionResult> TestAdmin()
        {
            var admin = await _context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
            if (admin != null)
            {
                return Ok(new
                {
                    message = "Admin kullanıcısı bulundu",
                    admin = new
                    {
                        admin.Id,
                        admin.Username,
                        admin.Name,
                        admin.Position,
                        admin.Email
                    }
                });
            }
            return NotFound("Admin kullanıcısı bulunamadı");
        }

        [Authorize]
        [HttpGet("protected")]
        public IActionResult ProtectedEndpoint()
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            var hasAuth = User.Identity?.IsAuthenticated ?? false;
            var userName = User.Identity?.Name ?? "null";

            return Ok(new
            {
                AuthHeader = authHeader,
                IsAuthenticated = hasAuth,
                UserName = userName,
                Claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
            });
        }


        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return NotFound("Kullanıcı bulunamadı");

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
    }
}