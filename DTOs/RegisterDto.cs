using System.ComponentModel.DataAnnotations;

namespace TaskSchedulingApp.DTOs
{
    public class RegisterDto
    {
        [Required]
        public string Username { get; set; }
        
        [Required]
        public string FirstName { get; set; }
        
        [Required]
        public string LastName { get; set; }

        [Required]
        public string Password { get; set; }
        
        [Required]
        public string Role {  get; set; }
    }
}