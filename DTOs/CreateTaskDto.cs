using System.Runtime.CompilerServices;
using TaskSchedulingApp.Attributes;

namespace TaskSchedulingApp.DTOs
{
    public class CreateTaskDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        [FutureDate(ErrorMessage = "Due date must be in the future")]
        public DateTime DueDate { get; set; }
        public IEnumerable<string> AssigneeUsernames { get; set; }

    }
}