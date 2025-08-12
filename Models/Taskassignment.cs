
namespace kebapbackend.Models
{
    public class Taskassignment
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string Task { get; set; } = null!;

        public Employee Employee { get; set; } = null!;
        public string? Name { get; set; }
    }
}
