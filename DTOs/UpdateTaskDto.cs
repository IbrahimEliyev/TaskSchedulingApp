using TaskSchedulingApp.Attributes;

namespace TaskSchedulingApp.DTOs
{
    [AlarmBeforeDue]
    public class UpdateTaskDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        [FutureDate(ErrorMessage = "Due date must be in the future")]
        public DateTime? DueDate { get; set; }
        public DateTime? AlarmDate { get; set; }
        public string? Status { get; set; }
        public IEnumerable<string>? AssigneeUsernames { get; set; }
    }
}