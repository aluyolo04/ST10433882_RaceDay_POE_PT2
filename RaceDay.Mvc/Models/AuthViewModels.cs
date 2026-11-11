
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Mvc.Models
{
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string Password { get; set; } = "";
    }

    public class RegisterViewModel
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = "";

        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        [MinLength(8)]
        public string Password { get; set; } = "";

        [Required]
        [Compare("Password")]
        public string ConfirmPassword { get; set; } = "";

        [Required]
        public string Role { get; set; } = "Participant";
    }

    public class LoginRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class RegisterRequest
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string Role { get; set; } = "";
    }

    public class AuthResponse
    {
        public string Token { get; set; } = "";
        public DateTime Expires { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
    }
}
