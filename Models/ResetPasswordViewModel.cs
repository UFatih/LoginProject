using Entities;

namespace LoginProject.Models
{
    public class ResetPasswordViewModel
    {
        public int Id { get; set; }
        public string? Token { get; set; }
        public string? NewPassword { get; set; }
        public string? ConfirmPassword { get; set; } 
    }
}
