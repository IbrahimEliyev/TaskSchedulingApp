using System.ComponentModel.DataAnnotations;
using TaskSchedulingApp.Models;

namespace TaskSchedulingApp.DTOs
{
    public class TaskResponseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? AlarmDate { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public List<string> AssigneeUsernames { get; set; }
    }
}