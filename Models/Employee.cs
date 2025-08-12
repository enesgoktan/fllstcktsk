using System.Collections.Generic;

namespace kebapbackend.Models
{
    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int Age { get; set; }
        public string Position { get; set; } = null!;
        public List<string> Fields { get; set; } = new List<string>();

        // User ile bire çok ilişki
        public ICollection<User> Users { get; set; } = new List<User>();

        // Taskassignment ile bire çok ilişki
        public ICollection<Taskassignment> Taskassignments { get; set; } = new List<Taskassignment>();
    }
}
