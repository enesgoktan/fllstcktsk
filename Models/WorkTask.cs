using System;
using System.ComponentModel.DataAnnotations;

namespace kebapbackend.Models
{
    public class WorkTask
    {
        [Key]
        public int Id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public DateTime DueDate { get; set; }

        public bool IsCompleted { get; set; }
    }
}
