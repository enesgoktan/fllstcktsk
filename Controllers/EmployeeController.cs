using kebapbackend.Data;
using kebapbackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.JSInterop.Infrastructure; // << EKLENDİ

namespace kebapbackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmployeeController : ControllerBase
    {
        private readonly AppDbContext _context;

        private readonly PasswordHasher<User> _passwordHasher = new PasswordHasher<User>(); // << EKLENDİ

        public EmployeeController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Employee
        [HttpGet]
        public async Task<ActionResult<List<Employee>>> Get()
        {
            return await _context.Employees.ToListAsync();
        }

        // GET: api/Employee/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Employee>> Get(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }
            return employee;
        }

        // POST: api/Employee - İşçi eklerken otomatik hesap oluştur
        [HttpPost]
        public async Task<ActionResult<object>> Post(Employee employee)
        {
            try
            {
                Console.WriteLine("🔥 POST METODU ÇALIŞTI!");
                Console.WriteLine($"Gelen veri: Name={employee.Name}, Position={employee.Position}, Age={employee.Age}");

                if (string.IsNullOrWhiteSpace(employee.Name) || string.IsNullOrWhiteSpace(employee.Position))
                {
                    Console.WriteLine("❌ Validation hatası: Name veya Position boş");
                    return BadRequest("Name and Position are required.");
                }

                // Employee'ı kaydet
                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();
                Console.WriteLine("✅ Employee kaydedildi");

                // Otomatik hesap oluştur
                var tempPassword = GenerateTempPassword();
                var generatedUsername = await GenerateUsername(employee.Name);

                Console.WriteLine("🚀 ŞİFRE OLUŞTURULDU!");
                Console.WriteLine($"=== YENİ ÇALIŞAN HESABI ===");
                Console.WriteLine($"İsim: {employee.Name}");
                Console.WriteLine($"Username: {generatedUsername}");
                Console.WriteLine($"Geçici Şifre: {tempPassword}");
                Console.WriteLine($"Tarih: {DateTime.Now}");
                Console.WriteLine($"==========================");
                var newUser = new User
                {
                    Username = generatedUsername,
                    Email = $"{generatedUsername}@gmail.com",
                    Name = employee.Name,
                    Position = employee.Position,
                    MustChangePassword = true,
                    TempPassword = tempPassword,
                    CreatedAt = DateTime.UtcNow,

                    EmployeeId = employee.Id
                };

                // Şifreyi hash'le
                newUser.Password = _passwordHasher.HashPassword(newUser, tempPassword);

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
                Console.WriteLine("✅ User hesabı da oluşturuldu");

                return Ok(new
                {
                    Employee = employee,
                    Account = new
                    {
                        Username = generatedUsername,
                        TempPassword = tempPassword,
                        Message = "Hesap otomatik oluşturuldu. İşçi ilk girişte şifresini değiştirmek zorunda."
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 HATA OLUŞTU: {ex.Message}");
                Console.WriteLine($"💥 STACK TRACE: {ex.StackTrace}");
                Console.WriteLine($"💥 INNER EXCEPTION: {ex.InnerException?.Message}");
                return StatusCode(500, $"Employee eklenirken hata oluştu: {ex.Message}");
            }
        }

        // PUT: api/Employee/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, Employee employee)
        {
            if (id != employee.Id)
            {
                return BadRequest("Id mismatch.");
            }

            if (string.IsNullOrWhiteSpace(employee.Name) || string.IsNullOrWhiteSpace(employee.Position))
            {
                return BadRequest("Name and Position are required.");
            }

            _context.Entry(employee).State = EntityState.Modified;

            // İlgili User hesabını da güncelle
            var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == employee.Id);
            if (user != null)
            {
                user.Name = employee.Name;
                user.Position = employee.Position;
                _context.Entry(user).State = EntityState.Modified;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Employees.AnyAsync(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Employee/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            // İlgili User hesabını da sil
            var users = await _context.Users.Where(u => u.EmployeeId == employee.Id).ToListAsync();
            _context.Users.RemoveRange(users);

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Geçici şifre oluştur
        private string GenerateTempPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        // Username oluştur (isimden)
        private async Task<string> GenerateUsername(string name)
        {
            var baseName = name.ToLower()
                .Replace(" ", "")
                .Replace("ş", "s")
                .Replace("ğ", "g")
                .Replace("ü", "u")
                .Replace("ö", "o")
                .Replace("ç", "c")
                .Replace("ı", "i");

            var counter = 1;
            var candidateUsername = baseName;

            while (await _context.Users.AnyAsync(u => u.Username == candidateUsername))
            {
                candidateUsername = $"{baseName}{counter}";
                counter++;
            }

            return candidateUsername;
        }
    }
}