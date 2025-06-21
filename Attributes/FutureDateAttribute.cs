using System.ComponentModel.DataAnnotations;

namespace TaskSchedulingApp.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class FutureDateAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if(value == null) return true;

            if(value is DateTime dateTime)
            {
                return dateTime > DateTime.UtcNow;
            }

            return false;
        }
    }
}