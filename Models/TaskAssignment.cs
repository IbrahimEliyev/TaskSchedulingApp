using System.ComponentModel.DataAnnotations.Schema;

namespace TaskSchedulingApp.Models
{
    public class TaskAssignment
    {
        public string UserId { get; set; }
        public User User { get; set; }

        public Guid TaskId { get; set; }
        public TaskItem TaskItem { get; set; } // or TaskItem Task
    }
}