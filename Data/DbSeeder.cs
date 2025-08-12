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
                    new User { Name = "Ahmet Y�lmaz", Email = "ahmet@example.com", Position = "manager" },
                    new User { Name = "Mehmet Demir", Email = "mehmet@example.com", Position = "manager" },
                    new User { Name = "Ay�e Kaya", Email = "ayse@example.com", Position = "employee" }
                );
            }

            if (!context.Employees.Any())
            {
                context.Employees.AddRange(
                    new Employee { Name = "Ahmet Y�lmaz", Age = 35, Position = "Manager" },
                    new Employee { Name = "Ay�e Kaya", Age = 28, Position = "Developer" }
                );
            }

            context.SaveChanges();
        }
    }
}