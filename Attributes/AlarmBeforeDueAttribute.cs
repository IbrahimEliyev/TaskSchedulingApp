using System.ComponentModel.DataAnnotations;
using TaskSchedulingApp.DTOs;

namespace TaskSchedulingApp.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AlarmBeforeDueAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            var dto = value as UpdateTaskDto;
            if (dto == null)
                return ValidationResult.Success;

            if (dto.AlarmDate != null && dto.DueDate != null)
            {
                if (dto.AlarmDate <= DateTime.UtcNow)
                {
                    return new ValidationResult("Alarm date must be in the future.");
                }

                if (dto.AlarmDate >= dto.DueDate)
                {
                    return new ValidationResult("Alarm date must be before due date.");
                }
            }

            return ValidationResult.Success;
        }
    }
}
