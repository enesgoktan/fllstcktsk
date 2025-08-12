using kebapbackend.Models;

namespace kebapbackend.Data
{
    public static class DbSeeder
    {
        public static void Seed(AppDbContext context)
        {
            if (!context.Users.Any())
            {
                context.Users.AddRange(
                    new User { Name = "Ahmet Yýlmaz", Email = "ahmet@example.com", Position = "manager" },
                    new User { Name = "Mehmet Demir", Email = "mehmet@example.com", Position = "manager" },
                    new User { Name = "Ayþe Kaya", Email = "ayse@example.com", Position = "employee" }
                );
            }

            if (!context.Employees.Any())
            {
                context.Employees.AddRange(
                    new Employee { Name = "Ahmet Yýlmaz", Age = 35, Position = "Manager" },
                    new Employee { Name = "Ayþe Kaya", Age = 28, Position = "Developer" }
                );
            }

            context.SaveChanges();
        }
    }
}