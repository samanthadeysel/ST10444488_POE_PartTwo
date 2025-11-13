using Microsoft.AspNetCore.Identity;

namespace ST10444488_POE.Models
{
    public class User : IdentityUser
    {
        public int UserId { get; set; }

        public string Username { get; set; }

        public string PasswordHash { get; set; }

        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        public string Role { get; set; }
    }
}
